using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Commander.Core;
using LogLib;

namespace Commander.ViewModel
{
    // The update flow (#6): check at startup or from Help, ask, then install and restart or open the release page.
    public class UpdateVM
    {
        public const string LastCheckKey = "updates.lastCheck", SkippedKey = "updates.skipped";
        private readonly UpdateChecker _checker;
        private readonly IDialogService _dialogs;
        private readonly IConfigReader _config;
        private readonly JobQueue _jobs;
        private readonly ILogger _logger;
        private readonly string _current;
        private readonly string _installDir;
        private readonly Func<bool> _canInstall;
        private readonly Action<string> _open;
        private readonly Action _restart;

        // canInstall, open and restart are seams for tests; the defaults touch the install folder, the browser and the process.
        public UpdateVM(UpdateChecker checker, IDialogService dialogs, IConfigReader config, JobQueue jobs, ILogger logger, string current,
            string installDir = null, Func<bool> canInstall = null, Action<string> open = null, Action restart = null)
        {
            _checker = checker;
            _dialogs = dialogs;
            _config = config;
            _jobs = jobs;
            _logger = logger;
            _current = current;
            _installDir = installDir ?? AppContext.BaseDirectory;
            _canInstall = canInstall ?? (() => Installer.IsPublished && Installer.IsWritable(_installDir));
            _open = open ?? (url => Shell.Open(url));
            _restart = restart ?? App.Restart;
        }

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        // manual: Help → Check for updates. It ignores the setting, the daily throttle and Skip, and reports "up to date".
        public async Task Check(bool manual)
        {
            if (!manual && (!AppSettings.Current.CheckForUpdates || _config.GetSetting(LastCheckKey) == Today))
                return;
            ReleaseInfo release;
            try
            {
                if (!manual)
                    _config.SaveSetting(LastCheckKey, Today);
                release = await Task.Run(() => _checker.Latest());
            }
            catch (Exception e)
            {
                if (manual) _logger.Error("Cannot check for updates: " + e.Message);
                else _logger.Debug("Update check failed: " + e.Message);
                return;
            }
            if (!UpdateChecker.IsNewer(release.Tag, _current))
            {
                if (manual) _logger.Info(string.Format("You are up to date ({0})", _current));
                return;
            }
            if (!manual && release.Tag == _config.GetSetting(SkippedKey))
                return;

            var asset = UpdateChecker.AssetFor(release, RuntimeInformation.RuntimeIdentifier);
            var sums = release.Assets.FirstOrDefault(a => a.Name == UpdateChecker.SumsName);
            var canInstall = asset != null && sums != null && _canInstall();
            var answer = await _dialogs.AskUpdate(new UpdateModel { Current = _current, Release = release, CanInstall = canInstall });
            switch (answer)
            {
                case UpdateAnswer.Skip:
                    _config.SaveSetting(SkippedKey, release.Tag);
                    break;
                case UpdateAnswer.Download when canInstall:
                    await Install(release, asset, sums);
                    break;
                case UpdateAnswer.Download:
                    _open(release.PageUrl);
                    break;
            }
        }

        private async Task Install(ReleaseInfo release, ReleaseAsset asset, ReleaseAsset sums)
        {
            var dir = Path.Combine(Path.GetTempPath(), "mmcommander-update");
            var installed = false;
            var job = new TransferJob("Downloading", asset.Name, j =>
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                try
                {
                    var archive = Path.Combine(dir, asset.Name);
                    var hashes = _checker.GetText(sums.Url, j.Token).GetAwaiter().GetResult();
                    _checker.Download(asset, archive, j, j.Token, j.Pause).GetAwaiter().GetResult();
                    j.Token.ThrowIfCancellationRequested();
                    Installer.Install(archive, UpdateChecker.ExpectedHash(hashes, asset.Name), _installDir);
                    installed = true;
                }
                finally
                {
                    try { Directory.Delete(dir, true); } catch (Exception) { }
                }
            });
            var done = _jobs.Enqueue(job);
            _dialogs.ShowProgress(job);
            try
            {
                await done;
            }
            catch (Exception e) when (!job.Token.IsCancellationRequested)
            {
                // Includes a checksum mismatch: nothing was installed, so the release page is the way forward.
                _logger.Error("Update failed: " + e.Message);
                _open(release.PageUrl);
                return;
            }
            catch (Exception)
            {
                // Cancelled from the progress window.
            }
            if (!installed)
            {
                _logger.Info("Update cancelled");
                return;
            }
            _logger.Info("Updated to " + release.Tag + ", restarting");
            _restart();
        }
    }
}
