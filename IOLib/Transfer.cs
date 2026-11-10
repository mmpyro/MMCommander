using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace IOLib
{
    // What to do when the target file already exists.
    public enum ConflictPolicy
    {
        Ask,
        OverwriteOlder,
        Skip,
        KeepBoth,
    }

    public enum ConflictDecision
    {
        Replace,
        Skip,
        KeepBoth,
        // Stop the whole transfer.
        Cancel,
    }

    public class ConflictAnswer
    {
        public ConflictAnswer(ConflictDecision decision, bool applyToAll = false)
        {
            Decision = decision;
            ApplyToAll = applyToAll;
        }

        public ConflictDecision Decision { get; }
        // "Do this for all remaining conflicts".
        public bool ApplyToAll { get; }
    }

    public class Conflict
    {
        public string SourcePath { get; init; }
        public string TargetPath { get; init; }
        public long SourceSize { get; init; }
        public long TargetSize { get; init; }
        public DateTime SourceTime { get; init; }
        public DateTime TargetTime { get; init; }
        public int Index { get; init; }
    }

    public class TransferOptions
    {
        public ConflictPolicy Policy { get; set; } = ConflictPolicy.Ask;
        public bool PreserveTimestamps { get; set; } = true;
        // Compare SHA-256 of source and target after each file.
        public bool Verify { get; set; }
        // Off: a symlink is recreated as a link. On: its target is copied.
        public bool FollowSymlinks { get; set; }
        // Optional filter for files inside copied folders ("Only matching filter").
        public Func<string, bool> Include { get; set; }
    }

    public class TransferProgress
    {
        public int FileIndex { get; init; }
        public int FileCount { get; init; }
        public string CurrentName { get; init; }
        public long FileBytes { get; init; }
        public long FileTotal { get; init; }
        public long Bytes { get; init; }
        public long TotalBytes { get; init; }
        public int Errors { get; init; }
    }

    public class TransferResult
    {
        public int Copied { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public bool Cancelled { get; set; }
    }

    // Blocks the worker between chunks while paused.
    public class PauseGate
    {
        private readonly ManualResetEventSlim _running = new ManualResetEventSlim(true);
        public bool IsPaused => !_running.IsSet;
        public void Pause() => _running.Reset();
        public void Resume() => _running.Set();
        public void Wait(CancellationToken token) => _running.Wait(token);
    }

    // Copies or moves files and folders with byte progress, pause, cancel and a conflict policy.
    public class TransferEngine
    {
        private const int ChunkSize = 1024 * 1024;

        private readonly TransferOptions _options;
        private readonly Func<Conflict, ConflictAnswer> _ask;
        private readonly IProgress<TransferProgress> _progress;
        private readonly PauseGate _pause;
        private readonly CancellationToken _token;
        private ConflictAnswer _forAll;
        private int _conflicts;

        public TransferEngine(TransferOptions options, Func<Conflict, ConflictAnswer> ask = null, IProgress<TransferProgress> progress = null,
            PauseGate pause = null, CancellationToken token = default)
        {
            _options = options ?? new TransferOptions();
            _ask = ask;
            _progress = progress;
            _pause = pause ?? new PauseGate();
            _token = token;
        }

        private sealed class Item
        {
            public string Source;
            public string Target;
            public bool IsDir;
            public bool IsLink;
            public long Size;
            // Top-level source this item belongs to (moves delete it after a clean copy).
            public int Root;
        }

        public TransferResult Copy(IEnumerable<string> sources, string destinationDir) => Run(sources, destinationDir, false);

        public TransferResult Move(IEnumerable<string> sources, string destinationDir) => Run(sources, destinationDir, true);

        private TransferResult Run(IEnumerable<string> sources, string destinationDir, bool move)
        {
            var result = new TransferResult();
            var roots = sources.Select(Path.GetFullPath).ToList();
            var dest = Path.GetFullPath(destinationDir);
            var plan = new List<Item>();
            var rootFailed = new bool[roots.Count];
            for (int r = 0; r < roots.Count; r++)
            {
                var src = roots[r];
                var target = Path.Combine(dest, Path.GetFileName(Path.TrimEndingDirectorySeparator(src)));
                if (Directory.Exists(src) && IsSameOrInside(dest, src))
                {
                    result.Errors.Add(string.Format("Cannot copy {0} into itself", src));
                    rootFailed[r] = true;
                    continue;
                }
                // A plain rename is enough for a move to a free name on the same volume.
                if (move && !File.Exists(target) && !Directory.Exists(target) && SameVolume(src, dest) && !string.Equals(src, target))
                {
                    try
                    {
                        if (Directory.Exists(src)) Directory.Move(src, target); else File.Move(src, target);
                        result.Copied++;
                        continue;
                    }
                    catch (IOException)
                    {
                        // Falls back to copy + delete below.
                    }
                }
                Plan(src, target, r, plan);
            }

            long total = plan.Where(i => !i.IsDir && !i.IsLink).Sum(i => i.Size), done = 0;
            int files = plan.Count(i => !i.IsDir), index = 0;
            try
            {
                foreach (var item in plan)
                {
                    _token.ThrowIfCancellationRequested();
                    if (item.IsDir && !item.IsLink)
                    {
                        Directory.CreateDirectory(item.Target);
                        continue;
                    }
                    index++;
                    try
                    {
                        var target = Resolve(item, ref result);
                        if (target == null)
                        {
                            done += item.Size;
                            rootFailed[item.Root] = true;
                            continue;
                        }
                        if (item.IsLink)
                            CopyLink(item.Source, target, item.IsDir);
                        else
                            CopyFile(item, target, done, total, index, files, result.Errors.Count);
                        result.Copied++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        result.Errors.Add(string.Format("{0}: {1}", item.Source, ex.Message));
                        rootFailed[item.Root] = true;
                    }
                    done += item.IsLink ? 0 : item.Size;
                }
            }
            catch (OperationCanceledException)
            {
                result.Cancelled = true;
                return result;
            }

            if (move)
                for (int r = 0; r < roots.Count; r++)
                    if (!rootFailed[r] && (File.Exists(roots[r]) || Directory.Exists(roots[r])) && plan.Any(i => i.Root == r))
                        DeleteSource(roots[r], result);
            return result;
        }

        private void Plan(string src, string target, int root, List<Item> plan)
        {
            var info = Directory.Exists(src) ? (FileSystemInfo)new DirectoryInfo(src) : new FileInfo(src);
            bool isLink = info.LinkTarget != null && !_options.FollowSymlinks;
            if (info is DirectoryInfo dir)
            {
                plan.Add(new Item { Source = src, Target = target, IsDir = true, IsLink = isLink, Root = root });
                if (isLink) return;
                foreach (var child in dir.EnumerateFileSystemInfos())
                    Plan(child.FullName, Path.Combine(target, child.Name), root, plan);
                return;
            }
            // The include filter only applies inside folders; a marked file is always copied.
            if (plan.Any(i => i.Root == root) && _options.Include != null && !_options.Include(src))
                return;
            plan.Add(new Item { Source = src, Target = target, IsLink = isLink, Size = isLink ? 0 : ((FileInfo)info).Length, Root = root });
        }

        // Applies the conflict policy; returns the path to write, or null to skip.
        private string Resolve(Item item, ref TransferResult result)
        {
            if (!File.Exists(item.Target) && !Directory.Exists(item.Target))
                return item.Target;
            var decision = Decide(item);
            switch (decision)
            {
                case ConflictDecision.Replace:
                    if (File.Exists(item.Target))
                        File.SetAttributes(item.Target, FileAttributes.Normal);
                    return item.Target;
                case ConflictDecision.KeepBoth:
                    return UniqueName(item.Target);
                case ConflictDecision.Cancel:
                    throw new OperationCanceledException();
                default:
                    result.Skipped++;
                    return null;
            }
        }

        private ConflictDecision Decide(Item item)
        {
            switch (_options.Policy)
            {
                case ConflictPolicy.Skip: return ConflictDecision.Skip;
                case ConflictPolicy.KeepBoth: return ConflictDecision.KeepBoth;
                case ConflictPolicy.OverwriteOlder:
                    return File.GetLastWriteTimeUtc(item.Source) > File.GetLastWriteTimeUtc(item.Target) ? ConflictDecision.Replace : ConflictDecision.Skip;
            }
            if (_forAll != null) return _forAll.Decision;
            if (_ask == null) return ConflictDecision.Skip;
            var target = new FileInfo(item.Target);
            var answer = _ask(new Conflict
            {
                SourcePath = item.Source,
                TargetPath = item.Target,
                SourceSize = item.Size,
                TargetSize = target.Exists ? target.Length : 0,
                SourceTime = File.GetLastWriteTime(item.Source),
                TargetTime = target.Exists ? target.LastWriteTime : Directory.GetLastWriteTime(item.Target),
                Index = ++_conflicts,
            }) ?? new ConflictAnswer(ConflictDecision.Skip);
            if (answer.ApplyToAll) _forAll = answer;
            return answer.Decision;
        }

        private void CopyFile(Item item, string target, long done, long total, int index, int files, int errors)
        {
            Report(item, 0, done, total, index, files, errors);
            var temp = target + ".mmc-part";
            try
            {
                using (var input = new FileStream(item.Source, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize))
                using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, ChunkSize))
                {
                    var buffer = new byte[ChunkSize];
                    long copied = 0;
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        _pause.Wait(_token);
                        _token.ThrowIfCancellationRequested();
                        output.Write(buffer, 0, read);
                        copied += read;
                        Report(item, copied, done + copied, total, index, files, errors);
                    }
                }
                File.Move(temp, target, true);
            }
            catch
            {
                // Never leave a half-written file behind (cancel, disk full, ...).
                if (File.Exists(temp)) File.Delete(temp);
                throw;
            }
            if (_options.PreserveTimestamps)
            {
                File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(item.Source));
                try { File.SetCreationTimeUtc(target, File.GetCreationTimeUtc(item.Source)); }
                catch (PlatformNotSupportedException) { }
            }
            if (_options.Verify && !SameHash(item.Source, target))
                throw new IOException("Verification failed: the copy differs from the source");
        }

        private void Report(Item item, long fileBytes, long bytes, long total, int index, int files, int errors) =>
            _progress?.Report(new TransferProgress
            {
                FileIndex = index,
                FileCount = files,
                CurrentName = Path.GetFileName(item.Source),
                FileBytes = fileBytes,
                FileTotal = item.Size,
                Bytes = bytes,
                TotalBytes = total,
                Errors = errors,
            });

        private static void CopyLink(string source, string target, bool isDir)
        {
            var linkTarget = (isDir ? (FileSystemInfo)new DirectoryInfo(source) : new FileInfo(source)).LinkTarget;
            if (File.Exists(target)) File.Delete(target);
            if (isDir) Directory.CreateSymbolicLink(target, linkTarget); else File.CreateSymbolicLink(target, linkTarget);
        }

        private static void DeleteSource(string path, TransferResult result)
        {
            try
            {
                if (Directory.Exists(path) && new DirectoryInfo(path).LinkTarget == null)
                {
                    foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                        File.SetAttributes(f, FileAttributes.Normal);
                    Directory.Delete(path, true);
                }
                else
                    File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Errors.Add(string.Format("Copied but could not remove {0}: {1}", path, ex.Message));
            }
        }

        // "name (1).ext", "name (2).ext", ... in the same folder.
        public static string UniqueName(string path)
        {
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            for (int i = 1; ; i++)
            {
                var candidate = Path.Combine(dir, string.Format("{0} ({1}){2}", name, i, ext));
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }
        }

        // Total bytes below a folder; unreadable parts are skipped.
        public static long FolderSize(string path, CancellationToken token = default)
        {
            long total = 0;
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
            foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", options))
            {
                token.ThrowIfCancellationRequested();
                total += file.Length;
            }
            return total;
        }

        public static bool SameHash(string a, string b)
        {
            using var sha = SHA256.Create();
            using var fa = File.OpenRead(a);
            var ha = sha.ComputeHash(fa);
            using var fb = File.OpenRead(b);
            return ha.AsSpan().SequenceEqual(sha.ComputeHash(fb));
        }

        private static bool SameVolume(string a, string b) =>
            string.Equals(Path.GetPathRoot(a), Path.GetPathRoot(b), StringComparison.OrdinalIgnoreCase);

        private static bool IsSameOrInside(string path, string dir)
        {
            var relative = Path.GetRelativePath(dir, path);
            return relative == "." || !(relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar) || Path.IsPathRooted(relative));
        }
    }
}
