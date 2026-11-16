using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Commander.Core;
using Commander.ViewModel;
using LogLib;
using NUnit.Framework;
using File = System.IO.File;

namespace CommanderTest
{
    // Answers each request from a map of URL -> response; no network.
    public class FakeHttp : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; }
        // Wins over Respond; gets HttpClient's token, so a timeout can cancel it.
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> RespondAsync { get; set; }
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return RespondAsync != null ? RespondAsync(request, token) : Task.FromResult(Respond(request));
        }

        public static HttpResponseMessage Ok(string text) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) };
        public static HttpResponseMessage Ok(byte[] bytes) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    }

    // A response body that arrives in chunks with a delay before each; counts the reads.
    public class SlowStream : Stream
    {
        private readonly int _chunks, _chunkSize, _delayMs;
        private int _reads;

        public SlowStream(int chunks, int chunkSize, int delayMs)
        {
            _chunks = chunks;
            _chunkSize = chunkSize;
            _delayMs = delayMs;
        }

        public int Reads => Volatile.Read(ref _reads);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            if (Reads >= _chunks) return 0;
            await Task.Delay(_delayMs, token);
            var n = Math.Min(_chunkSize, buffer.Length);
            buffer.Span.Slice(0, n).Fill(1);
            Interlocked.Increment(ref _reads);
            return n;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) =>
            ReadAsync(buffer.AsMemory(offset, count), token).AsTask();

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // Auto updater (#6): release check, version compare, checksum, install swap, and the dialog flow.
    [TestFixture]
    public class UpdaterTest
    {
        private const string Api = "https://api.github.com/repos/mmpyro/MMCommander/releases/latest";
        private static readonly string Rid = RuntimeInformation.RuntimeIdentifier;
        private string _root, _install;

        [SetUp]
        public void Before()
        {
            // A fresh Current: UI tests leave UI-thread listeners on the shared one, and these tests run off the UI thread.
            AppSettings.Load(null);
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-update-" + Guid.NewGuid().ToString("N"))).FullName;
            _install = Directory.CreateDirectory(Path.Combine(_root, "install")).FullName;
            File.WriteAllText(Path.Combine(_install, "Commander"), "old binary");
            File.WriteAllText(Path.Combine(_install, "Config.xml"), "old config");
            Directory.CreateDirectory(Path.Combine(_install, "Plugins", "Mine"));
            File.WriteAllText(Path.Combine(_install, "Plugins", "Mine", "Mine.dll"), "user plugin");
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private static string ReleaseJson(string tag, string notes = "Fixes", params string[] assets) => JsonSerializer.Serialize(new
        {
            tag_name = tag,
            body = notes,
            html_url = "https://github.com/mmpyro/MMCommander/releases/tag/" + tag,
            assets = assets.Select(name => new { name, browser_download_url = "https://github.com/mmpyro/MMCommander/releases/download/" + tag + "/" + name, size = 10 }),
        });

        [TestCase("v2.2.0", "2.1.0", true)]
        [TestCase("2.2.0", "2.1.0", true)]
        [TestCase("v2.1.0", "2.1.0", false)]
        [TestCase("v2.0.9", "2.1.0", false)]
        [TestCase("v2.10.0", "2.9.0", true)]
        [TestCase("v3.0.0-beta", "2.1.0", false)]
        // T4: the same release written with four parts is not an update.
        [TestCase("v2.1.0.0", "2.1.0", false)]
        [TestCase("v2.1", "2.1.0", false)]
        [TestCase("v2.2", "2.1.0", true)]
        [TestCase("latest", "2.1.0", false)]
        [TestCase("", "2.1.0", false)]
        [TestCase(null, "2.1.0", false)]
        public void IsNewer_ComparesVersions(string tag, string current, bool newer)
        {
            Assert.That(UpdateChecker.IsNewer(tag, current), Is.EqualTo(newer));
        }

        [Test]
        public async Task Latest_ParsesRelease()
        {
            var http = new FakeHttp { Respond = r => r.RequestUri.ToString() == Api ? FakeHttp.Ok(ReleaseJson("v2.2.0", "New things", "MMCommander-2.2.0-osx-arm64.tar.gz", "SHA256SUMS.txt")) : null };
            var release = await new UpdateChecker(http).Latest();
            Assert.That(release.Tag, Is.EqualTo("v2.2.0"));
            Assert.That(release.Notes, Is.EqualTo("New things"));
            Assert.That(release.PageUrl, Is.EqualTo("https://github.com/mmpyro/MMCommander/releases/tag/v2.2.0"));
            Assert.That(release.Assets.Select(a => a.Name), Is.EqualTo(new[] { "MMCommander-2.2.0-osx-arm64.tar.gz", "SHA256SUMS.txt" }));
        }

        [Test]
        public async Task Latest_NonGitHubLinks_AreNotTrusted()
        {
            var json = "{\"tag_name\":\"v9.0.0\",\"body\":null,\"html_url\":\"file:///bin/sh\",\"assets\":[{\"name\":\"x-linux-x64.tar.gz\",\"browser_download_url\":\"http://evil.example/x\",\"size\":1}]}";
            var release = await new UpdateChecker(new FakeHttp { Respond = _ => FakeHttp.Ok(json) }).Latest();
            Assert.That(release.PageUrl, Is.EqualTo(UpdateChecker.ReleasesPage));
            Assert.That(release.Assets, Is.Empty);
            Assert.That(release.Notes, Is.Empty);
        }

        // T17: a look-alike host must not pass the "https://github.com/" check.
        [Test]
        public async Task Latest_LookAlikeHost_IsNotTrusted()
        {
            var json = "{\"tag_name\":\"v9.0.0\",\"body\":\"\",\"html_url\":\"https://github.com.evil.example/x\",\"assets\":[{\"name\":\"x-linux-x64.tar.gz\",\"browser_download_url\":\"https://github.com.evil.example/x.tar.gz\",\"size\":1}]}";
            var release = await new UpdateChecker(new FakeHttp { Respond = _ => FakeHttp.Ok(json) }).Latest();
            Assert.That(release.PageUrl, Is.EqualTo(UpdateChecker.ReleasesPage));
            Assert.That(release.Assets, Is.Empty);
        }

        [Test]
        public async Task Latest_RateLimited_Throws()
        {
            var http = new FakeHttp { Respond = _ => new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("rate limit") } };
            await Assert.ThatAsync(() => new UpdateChecker(http).Latest(), Throws.InstanceOf<HttpRequestException>());
        }

        [TestCase("not json")]
        [TestCase("{\"message\":\"Not Found\"}")]
        public async Task Latest_Malformed_Throws(string body)
        {
            var http = new FakeHttp { Respond = _ => FakeHttp.Ok(body) };
            await Assert.ThatAsync(() => new UpdateChecker(http).Latest(), Throws.Exception);
        }

        [TestCase("win-x64", "MMCommander-2.2.0-win-x64.zip")]
        [TestCase("linux-x64", "MMCommander-2.2.0-linux-x64.tar.gz")]
        [TestCase("osx-arm64", "MMCommander-2.2.0-osx-arm64.tar.gz")]
        [TestCase("osx-x64", null)]
        public void AssetFor_MatchesRid(string rid, string expected)
        {
            var release = new ReleaseInfo
            {
                Assets = new[] { "MMCommander-2.2.0-win-x64.zip", "MMCommander-2.2.0-linux-x64.tar.gz", "MMCommander-2.2.0-osx-arm64.tar.gz", "SHA256SUMS.txt" }
                    .Select(n => new ReleaseAsset { Name = n }).ToList(),
            };
            Assert.That(UpdateChecker.AssetFor(release, rid)?.Name, Is.EqualTo(expected));
        }

        [Test]
        public void ExpectedHash_ReadsSha256SumsFormat()
        {
            var sums = "AAA111  MMCommander-2.2.0-win-x64.zip\nbbb222 *MMCommander-2.2.0-linux-x64.tar.gz\n";
            Assert.That(UpdateChecker.ExpectedHash(sums, "MMCommander-2.2.0-win-x64.zip"), Is.EqualTo("aaa111"));
            Assert.That(UpdateChecker.ExpectedHash(sums, "MMCommander-2.2.0-linux-x64.tar.gz"), Is.EqualTo("bbb222"));
            Assert.That(UpdateChecker.ExpectedHash(sums, "MMCommander-2.2.0-osx-arm64.tar.gz"), Is.Null);
        }

        // A release archive like release.yml makes: one top folder with the app files.
        private string MakeZip(string name = "MMCommander-9.9.9-test.zip")
        {
            var package = Directory.CreateDirectory(Path.Combine(_root, "pkg", "MMCommander-9.9.9-test")).FullName;
            File.WriteAllText(Path.Combine(package, "Commander"), "new binary");
            File.WriteAllText(Path.Combine(package, "Config.xml"), "new config");
            Directory.CreateDirectory(Path.Combine(package, "Manuals"));
            File.WriteAllText(Path.Combine(package, "Manuals", "syntax.html"), "manual");
            var zip = Path.Combine(_root, name);
            ZipFile.CreateFromDirectory(package, zip, CompressionLevel.Fastest, includeBaseDirectory: true);
            Directory.Delete(Path.Combine(_root, "pkg"), true);
            return zip;
        }

        private Dictionary<string, string> Snapshot() => Directory.GetFiles(_install, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(_install, f), File.ReadAllText);

        [Test]
        public void Install_ChecksumMismatch_LeavesInstallUntouched()
        {
            var zip = MakeZip();
            var before = Snapshot();
            Assert.Throws<InvalidDataException>(() => Installer.Install(zip, new string('0', 64), _install));
            Assert.Throws<InvalidDataException>(() => Installer.Install(zip, null, _install));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void Install_ReplacesFiles_KeepsUserFiles_AndCleanupRemovesOld()
        {
            var zip = MakeZip();
            Installer.Install(zip, Installer.Sha256(zip).ToUpperInvariant(), _install);

            Assert.That(File.ReadAllText(Path.Combine(_install, "Commander")), Is.EqualTo("new binary"));
            Assert.That(File.ReadAllText(Path.Combine(_install, "Config.xml")), Is.EqualTo("new config"));
            Assert.That(File.ReadAllText(Path.Combine(_install, "Manuals", "syntax.html")), Is.EqualTo("manual"));
            Assert.That(File.ReadAllText(Path.Combine(_install, "Plugins", "Mine", "Mine.dll")), Is.EqualTo("user plugin"));
            Assert.That(File.ReadAllText(Path.Combine(_install, "Commander.old")), Is.EqualTo("old binary"), "kept for one run");

            File.WriteAllText(Path.Combine(_install, "notes.old"), "a user file, not left by an update");
            Installer.CleanupOld(_install);
            Assert.That(File.Exists(Path.Combine(_install, "Commander.old")), Is.False);
            Assert.That(File.Exists(Path.Combine(_install, "Config.xml.old")), Is.False);
            Assert.That(File.Exists(Path.Combine(_install, "notes.old")), Is.True);
        }

        [Test]
        public void Install_TarGz_KeepsExecutableBit()
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.Ignore("Windows releases are zip files and have no mode bits.");
                return;
            }
            var package = Directory.CreateDirectory(Path.Combine(_root, "pkg", "MMCommander-9.9.9-test")).FullName;
            var binary = Path.Combine(package, "Commander");
            File.WriteAllText(binary, "new binary");
            File.SetUnixFileMode(binary, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var archive = Path.Combine(_root, "MMCommander-9.9.9-test.tar.gz");
            using (var file = File.Create(archive))
            using (var gzip = new GZipStream(file, CompressionMode.Compress))
                TarFile.CreateFromDirectory(Path.Combine(_root, "pkg"), gzip, false);

            Installer.Install(archive, Installer.Sha256(archive), _install);

            var installed = Path.Combine(_install, "Commander");
            Assert.That(File.ReadAllText(installed), Is.EqualTo("new binary"));
            Assert.That(File.GetUnixFileMode(installed).HasFlag(UnixFileMode.UserExecute), Is.True);
        }

        [Test]
        public void Swap_Failure_RestoresOldFiles()
        {
            var package = Directory.CreateDirectory(Path.Combine(_root, "new")).FullName;
            File.WriteAllText(Path.Combine(package, "Commander"), "new binary");
            // "sub/blocked" is a folder in the install, so moving a file there fails after Commander was replaced.
            Directory.CreateDirectory(Path.Combine(package, "sub"));
            File.WriteAllText(Path.Combine(package, "sub", "blocked"), "x");
            Directory.CreateDirectory(Path.Combine(_install, "sub", "blocked"));
            var before = Snapshot();

            Assert.Catch<IOException>(() => Installer.Swap(package, _install));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        // The flow, with a fake GitHub and a throwaway user config.
        private (UpdateVM vm, FakeDialogs dialogs, FakeHttp http, ConfigReader config, List<string> log, List<string> opened, Func<bool> restarted) Flow(
            string tag, Func<HttpRequestMessage, HttpResponseMessage> files = null, bool canInstall = false)
        {
            var defaults = Path.Combine(_root, "Config.xml");
            if (!File.Exists(defaults))
                File.Copy(Path.Combine(AppContext.BaseDirectory, "Config.xml"), defaults);
            var config = new ConfigReader(new ShortcutManager(), defaults, Path.Combine(_root, "user", "Config.xml"));
            var assetName = "MMCommander-" + tag.TrimStart('v') + "-" + Rid + ".zip";
            var http = new FakeHttp
            {
                Respond = r => r.RequestUri.ToString() == Api ? FakeHttp.Ok(ReleaseJson(tag, "notes", assetName, UpdateChecker.SumsName)) : files?.Invoke(r),
            };
            var logger = new GUILogger();
            var log = new List<string>();
            logger.NotifyEvent += (_, m) => log.Add(m);
            var dialogs = new FakeDialogs();
            var opened = new List<string>();
            var restarted = false;
            var vm = new UpdateVM(new UpdateChecker(http), dialogs, config, new JobQueue(new StateMonitor()), logger, "2.1.0",
                _install, () => canInstall, opened.Add, () => restarted = true);
            return (vm, dialogs, http, config, log, opened, () => restarted);
        }

        [Test]
        public async Task Startup_NewerRelease_AsksOnce_PerDay()
        {
            var f = Flow("v2.2.0");
            await f.vm.Check(false);
            Assert.That(f.dialogs.LastUpdate?.Release.Tag, Is.EqualTo("v2.2.0"));
            Assert.That(f.dialogs.LastUpdate.Current, Is.EqualTo("2.1.0"));
            Assert.That(f.dialogs.LastUpdate.CanInstall, Is.False);

            await f.vm.Check(false);
            Assert.That(f.http.Calls, Is.EqualTo(1), "second start on the same day makes no call");
        }

        [TestCase("v2.1.0")]
        [TestCase("v2.0.0")]
        public async Task Startup_SameOrOlder_NoDialog(string tag)
        {
            var f = Flow(tag);
            await f.vm.Check(false);
            Assert.That(f.http.Calls, Is.EqualTo(1));
            Assert.That(f.dialogs.LastUpdate, Is.Null);
            Assert.That(f.log, Is.Empty);
        }

        [Test]
        public async Task Skip_IsRemembered_ButManualCheckStillAsks()
        {
            var f = Flow("v2.2.0");
            f.dialogs.UpdateAnswer = UpdateAnswer.Skip;
            await f.vm.Check(true);
            Assert.That(f.config.GetSetting(UpdateVM.SkippedKey), Is.EqualTo("v2.2.0"));

            // A restart: new reader on the same user config.
            var again = Flow("v2.2.0");
            Assert.That(again.config.GetSetting(UpdateVM.SkippedKey), Is.EqualTo("v2.2.0"));
            await again.vm.Check(false);
            Assert.That(again.dialogs.LastUpdate, Is.Null);
            await again.vm.Check(true);
            Assert.That(again.dialogs.LastUpdate, Is.Not.Null);
        }

        [Test]
        public async Task SettingOff_MakesNoNetworkCall()
        {
            var f = Flow("v2.2.0");
            var saved = AppSettings.Current.CheckForUpdates;
            AppSettings.Current.CheckForUpdates = false;
            try
            {
                await f.vm.Check(false);
            }
            finally
            {
                AppSettings.Current.CheckForUpdates = saved;
            }
            Assert.That(f.http.Calls, Is.Zero);
            Assert.That(f.dialogs.LastUpdate, Is.Null);
        }

        [Test]
        public async Task Offline_IsSilentAtStartup_AndLoggedWhenManual()
        {
            var f = Flow("v2.2.0");
            f.http.Respond = _ => throw new HttpRequestException("No such host is known");
            await f.vm.Check(false);
            Assert.That(f.dialogs.LastUpdate, Is.Null);
            Assert.That(f.log, Is.Empty);

            await f.vm.Check(true);
            Assert.That(f.log.Single(), Does.Contain("Cannot check for updates: No such host is known"));
        }

        [Test]
        public async Task Manual_UpToDate_IsReported()
        {
            var f = Flow("v2.1.0");
            await f.vm.Check(true);
            Assert.That(f.log.Single(), Does.Contain("You are up to date (2.1.0)"));
            Assert.That(f.dialogs.LastUpdate, Is.Null);
        }

        [Test]
        public async Task Download_WhenCannotInstall_OpensReleasePage()
        {
            var f = Flow("v2.2.0");
            f.dialogs.UpdateAnswer = UpdateAnswer.Download;
            await f.vm.Check(true);
            Assert.That(f.opened, Is.EqualTo(new[] { "https://github.com/mmpyro/MMCommander/releases/tag/v2.2.0" }));
            Assert.That(f.restarted(), Is.False);
        }

        [Test]
        public async Task Later_DoesNothing()
        {
            var f = Flow("v2.2.0");
            f.dialogs.UpdateAnswer = UpdateAnswer.Later;
            await f.vm.Check(true);
            Assert.That(f.opened, Is.Empty);
            Assert.That(f.config.GetSetting(UpdateVM.SkippedKey), Is.Null);
        }

        private Func<HttpRequestMessage, HttpResponseMessage> Files(byte[] zip, string hash)
        {
            var name = "MMCommander-2.2.0-" + Rid + ".zip";
            return r => r.RequestUri.AbsolutePath.EndsWith("/" + UpdateChecker.SumsName) ? FakeHttp.Ok(hash + "  " + name + "\n")
                : r.RequestUri.AbsolutePath.EndsWith("/" + name) ? FakeHttp.Ok(zip)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Download_Installs_AndRestarts()
        {
            var zip = MakeZip();
            var f = Flow("v2.2.0", Files(File.ReadAllBytes(zip), Installer.Sha256(zip)), canInstall: true);
            f.dialogs.UpdateAnswer = UpdateAnswer.Download;
            await f.vm.Check(true);

            Assert.That(f.dialogs.LastUpdate.CanInstall, Is.True);
            Assert.That(File.ReadAllText(Path.Combine(_install, "Commander")), Is.EqualTo("new binary"));
            Assert.That(f.restarted(), Is.True);
            Assert.That(f.opened, Is.Empty);
        }

        [Test]
        public async Task Download_ChecksumMismatch_KeepsInstall_AndOpensPage()
        {
            var zip = MakeZip();
            var before = Snapshot();
            var f = Flow("v2.2.0", Files(File.ReadAllBytes(zip), new string('a', 64)), canInstall: true);
            f.dialogs.UpdateAnswer = UpdateAnswer.Download;
            await f.vm.Check(true);

            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(f.restarted(), Is.False);
            Assert.That(f.log, Has.Some.Contains("Update failed: Checksum mismatch"));
            Assert.That(f.opened, Has.Count.EqualTo(1));
        }

        private static async Task WaitFor(Func<bool> done, int timeoutMs = 5000)
        {
            var clock = Stopwatch.StartNew();
            while (!done() && clock.ElapsedMilliseconds < timeoutMs)
                await Task.Delay(20);
            Assert.That(done(), Is.True, "timed out waiting");
        }

        // T6: a GitHub that never answers gives up at the 5 s timeout, silently.
        [Test]
        public async Task Startup_SlowGitHub_GivesUpAtTimeout()
        {
            var f = Flow("v2.2.0");
            f.http.RespondAsync = async (_, token) => { await Task.Delay(TimeSpan.FromSeconds(30), token); return FakeHttp.Ok("{}"); };
            var clock = Stopwatch.StartNew();
            await f.vm.Check(false);
            Assert.That(clock.Elapsed.TotalSeconds, Is.InRange(4.5, 7.0));
            Assert.That(f.dialogs.LastUpdate, Is.Null);
            Assert.That(f.log, Is.Empty);
        }

        // T24: the 5 s timeout must not cut off a download whose body takes longer.
        [Test]
        public async Task Download_SlowBody_IsNotCutAtTimeout()
        {
            var http = new FakeHttp { Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new SlowStream(8, 1000, 900)) } };
            var file = Path.Combine(_root, "slow.bin");
            await new UpdateChecker(http).Download(new ReleaseAsset { Name = "slow.bin", Url = "https://github.com/x/slow.bin", Size = 8000 }, file, null, CancellationToken.None);
            Assert.That(new FileInfo(file).Length, Is.EqualTo(8000));
        }

        private (UpdateVM vm, FakeDialogs dialogs, FakeHttp http, ConfigReader config, List<string> log, List<string> opened, Func<bool> restarted) SlowInstall(SlowStream body) =>
            Flow("v2.2.0", r => r.RequestUri.AbsolutePath.EndsWith("/" + UpdateChecker.SumsName)
                ? FakeHttp.Ok(new string('a', 64) + "  MMCommander-2.2.0-" + Rid + ".zip\n")
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) }, canInstall: true);

        // T23: Cancel in the progress window stops the download and changes nothing.
        [Test]
        public async Task Download_Cancelled_LeavesInstall_AndOpensNothing()
        {
            var body = new SlowStream(1000, 1000, 20);
            var f = SlowInstall(body);
            f.dialogs.UpdateAnswer = UpdateAnswer.Download;
            var before = Snapshot();
            var check = f.vm.Check(true);
            await WaitFor(() => body.Reads >= 3);
            f.dialogs.LastJob.Cancel();
            Assert.That(await Task.WhenAny(check, Task.Delay(10000)), Is.SameAs(check), "the flow ends after Cancel");

            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(f.log, Has.Some.Contains("Update cancelled"));
            Assert.That(f.opened, Is.Empty);
            Assert.That(f.restarted(), Is.False);
            Assert.That(Directory.Exists(Path.Combine(Path.GetTempPath(), "mmcommander-update")), Is.False, "temp download removed");
        }

        // T25: Pause in the progress window should stop the download until Resume.
        [Test]
        public async Task Download_Pause_StopsReading()
        {
            var body = new SlowStream(1000, 1000, 20);
            var f = SlowInstall(body);
            f.dialogs.UpdateAnswer = UpdateAnswer.Download;
            var check = f.vm.Check(true);
            await WaitFor(() => body.Reads >= 3);
            f.dialogs.LastJob.PauseCommand.Execute(null);
            await Task.Delay(300);
            var atPause = body.Reads;
            await Task.Delay(1000);
            var afterSecond = body.Reads;
            f.dialogs.LastJob.Cancel();
            await Task.WhenAny(check, Task.Delay(10000));
            Assert.That(afterSecond, Is.EqualTo(atPause), "reads went on while paused");
        }

        // T26: the real latest release from GitHub installs into a temp folder (real zip layout, GNU/BSD tar, sums format).
        [Explicit("Downloads the real release archives from GitHub (about 160 MB)")]
        [TestCase("win-x64")]
        [TestCase("linux-x64")]
        [TestCase("osx-arm64")]
        public async Task RealRelease_InstallsIntoTempFolder(string rid)
        {
            var checker = new UpdateChecker();
            var release = await checker.Latest();
            var asset = UpdateChecker.AssetFor(release, rid);
            Assert.That(asset, Is.Not.Null, "no asset for " + rid);
            var hashes = await checker.GetText(release.Assets.Single(a => a.Name == UpdateChecker.SumsName).Url, CancellationToken.None);
            var archive = Path.Combine(Directory.CreateDirectory(Path.Combine(_root, "dl")).FullName, asset.Name);
            await checker.Download(asset, archive, null, CancellationToken.None);

            Installer.Install(archive, UpdateChecker.ExpectedHash(hashes, asset.Name), _install);

            var exe = Path.Combine(_install, rid.StartsWith("win") ? "Commander.exe" : "Commander");
            Assert.That(new FileInfo(exe).Length, Is.GreaterThan(1_000_000), "the app binary is at the top of the install folder");
            Assert.That(File.ReadAllText(Path.Combine(_install, "Config.xml")), Does.Contain("<Configuration"), "Config.xml replaced");
            Assert.That(File.Exists(Path.Combine(_install, "Manuals", "syntax.html")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(_install, "Plugins", "Mine", "Mine.dll")), Is.EqualTo("user plugin"));
            if (!rid.StartsWith("win") && !OperatingSystem.IsWindows())
                Assert.That(File.GetUnixFileMode(exe).HasFlag(UnixFileMode.UserExecute), Is.True);
        }
    }
}
