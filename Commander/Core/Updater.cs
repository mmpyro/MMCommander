using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IOLib;

namespace Commander.Core
{
    public class ReleaseAsset
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public long Size { get; set; }
    }

    public class ReleaseInfo
    {
        public string Tag { get; set; }
        public string Notes { get; set; }
        public string PageUrl { get; set; }
        public List<ReleaseAsset> Assets { get; set; } = new List<ReleaseAsset>();
    }

    // Esc closes the dialog with default(UpdateAnswer), so Later must stay 0.
    public enum UpdateAnswer { Later, Skip, Download }

    public class UpdateModel
    {
        public string Current { get; set; }
        public ReleaseInfo Release { get; set; }
        // True: Download installs and restarts. False: it opens the release page.
        public bool CanInstall { get; set; }
    }

    // Asks GitHub for the latest release and downloads its files (#6).
    public class UpdateChecker
    {
        public const string Repo = "mmpyro/MMCommander";
        public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";
        public const string SumsName = "SHA256SUMS.txt";
        private const string LatestUrl = "https://api.github.com/repos/" + Repo + "/releases/latest";
        private readonly HttpClient _http;

        public UpdateChecker(HttpMessageHandler handler = null)
        {
            // The timeout covers the release check and the download headers; a body read with ResponseHeadersRead is not limited.
            _http = new HttpClient(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromSeconds(5) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MMCommander");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        // Throws on network errors, non-2xx (403 rate limit) and unexpected JSON; the caller logs.
        public async Task<ReleaseInfo> Latest(CancellationToken token = default)
        {
            using var response = await _http.GetAsync(LatestUrl, token);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var root = doc.RootElement;
            var page = root.TryGetProperty("html_url", out var html) ? html.GetString() : null;
            return new ReleaseInfo
            {
                Tag = root.GetProperty("tag_name").GetString(),
                Notes = root.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String ? body.GetString() : "",
                // Shell.Open runs whatever it gets, so only a GitHub https link is trusted.
                PageUrl = IsGitHub(page) ? page : ReleasesPage,
                Assets = root.GetProperty("assets").EnumerateArray()
                    .Select(a => new ReleaseAsset
                    {
                        Name = a.GetProperty("name").GetString(),
                        Url = a.GetProperty("browser_download_url").GetString(),
                        Size = a.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                    })
                    .Where(a => IsGitHub(a.Url))
                    .ToList(),
            };
        }

        private static bool IsGitHub(string url) => url != null && url.StartsWith("https://github.com/", StringComparison.Ordinal);

        // "v2.1.0" -> 2.1.0; pre-release ("2.2.0-beta") and junk -> null, so they never count as newer.
        public static Version ParseTag(string tag)
        {
            var text = (tag ?? "").Trim().TrimStart('v', 'V');
            if (!Version.TryParse(text, out var v))
                return null;
            // Missing parts count as 0, so "2.1", "2.1.0" and "2.1.0.0" are the same version.
            return new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
        }

        public static bool IsNewer(string tag, string current)
        {
            var latest = ParseTag(tag);
            var running = ParseTag(current);
            return latest != null && running != null && latest > running;
        }

        // rid as in RuntimeInformation.RuntimeIdentifier, e.g. "osx-arm64".
        public static ReleaseAsset AssetFor(ReleaseInfo release, string rid) =>
            release.Assets.FirstOrDefault(a => a.Name.EndsWith("-" + rid + ".zip", StringComparison.OrdinalIgnoreCase)
                                            || a.Name.EndsWith("-" + rid + ".tar.gz", StringComparison.OrdinalIgnoreCase));

        // A line of `sha256sum` output: "<hex>  <name>" (a '*' before the name marks binary mode).
        public static string ExpectedHash(string sums, string fileName)
        {
            foreach (var line in (sums ?? "").Split('\n'))
            {
                var parts = line.Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[1].Trim().TrimStart('*') == fileName)
                    return parts[0].ToLowerInvariant();
            }
            return null;
        }

        public async Task<string> GetText(string url, CancellationToken token)
        {
            using var response = await _http.GetAsync(url, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(token);
        }

        // pause: the progress window's Pause button; blocks between chunks until Resume or Cancel.
        public async Task Download(ReleaseAsset asset, string file, IProgress<TransferProgress> progress, CancellationToken token, PauseGate pause = null)
        {
            using var response = await _http.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? asset.Size;
            using var input = await response.Content.ReadAsStreamAsync(token);
            using var output = File.Create(file);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, token)) > 0)
            {
                pause?.Wait(token);
                await output.WriteAsync(buffer.AsMemory(0, read), token);
                done += read;
                progress?.Report(new TransferProgress { FileIndex = 1, FileCount = 1, CurrentName = asset.Name, FileBytes = done, FileTotal = total, Bytes = done, TotalBytes = total });
            }
        }
    }

    // Replaces the install folder's files with a downloaded release. No helper process: a running file
    // (even Windows' exe and loaded DLLs) can be renamed, so each old file becomes "<name>.old" and the next start deletes it.
    public static class Installer
    {
        public const string OldSuffix = ".old";

        // Single-file publish leaves Location empty; `dotnet run` and tests must never overwrite their bin folder.
        public static bool IsPublished => string.IsNullOrEmpty(typeof(Installer).Assembly.Location);

        public static bool IsWritable(string dir)
        {
            try
            {
                var probe = Path.Combine(dir, ".mmc-write-test");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Sha256(string file)
        {
            using var stream = File.OpenRead(file);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }

        // Verifies first, so a bad download never touches installDir.
        public static void Install(string archive, string expectedHash, string installDir)
        {
            if (string.IsNullOrEmpty(expectedHash) || !string.Equals(Sha256(archive), expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Checksum mismatch for " + Path.GetFileName(archive));
            var unpacked = Path.Combine(Path.GetDirectoryName(archive), "unpacked");
            if (Directory.Exists(unpacked))
                Directory.Delete(unpacked, true);
            Directory.CreateDirectory(unpacked);
            Extract(archive, unpacked);
            Swap(PackageRoot(unpacked), installDir);
        }

        private static void Extract(string archive, string dest)
        {
            if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(archive, dest);
                return;
            }
            using var file = File.OpenRead(archive);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            // Keeps the Unix mode bits, so the binary stays executable.
            TarFile.ExtractToDirectory(gzip, dest, false);
        }

        // Release archives hold one top folder, "MMCommander-<ver>-<rid>/".
        private static string PackageRoot(string dir)
        {
            var dirs = Directory.GetDirectories(dir);
            return dirs.Length == 1 && Directory.GetFiles(dir).Length == 0 ? dirs[0] : dir;
        }

        // File by file, so files only the user added (their own plugins) stay. Any failure puts the old files back.
        public static void Swap(string newDir, string installDir)
        {
            var renamed = new List<string>();
            var placed = new List<string>();
            try
            {
                foreach (var source in Directory.GetFiles(newDir, "*", SearchOption.AllDirectories))
                {
                    var target = Path.Combine(installDir, Path.GetRelativePath(newDir, source));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    if (File.Exists(target))
                    {
                        File.Move(target, target + OldSuffix, true);
                        renamed.Add(target);
                    }
                    File.Move(source, target);
                    placed.Add(target);
                }
            }
            catch (Exception)
            {
                foreach (var target in Enumerable.Reverse(placed))
                    Try(() => File.Delete(target));
                foreach (var target in Enumerable.Reverse(renamed))
                    Try(() => File.Move(target + OldSuffix, target, true));
                throw;
            }
        }

        // Deletes "<name>.old" left by Swap, only where "<name>" exists. A file still locked (the old process
        // exiting on Windows) stays until the next start.
        public static void CleanupOld(string installDir) => Try(() =>
        {
            foreach (var old in Directory.GetFiles(installDir, "*" + OldSuffix, SearchOption.AllDirectories))
                // EndsWith: on Windows "*.old" also matches "x.older".
                if (old.EndsWith(OldSuffix, StringComparison.OrdinalIgnoreCase) && File.Exists(old.Substring(0, old.Length - OldSuffix.Length)))
                    Try(() => File.Delete(old));
        });

        private static void Try(Action action)
        {
            try { action(); }
            catch (Exception) { }
        }
    }
}
