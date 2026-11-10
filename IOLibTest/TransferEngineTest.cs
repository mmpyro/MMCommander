using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class TransferEngineTest
    {
        private string _root, _src, _dst;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, "transfer-" + Guid.NewGuid().ToString("N"))).FullName;
            _src = Directory.CreateDirectory(Path.Combine(_root, "src")).FullName;
            _dst = Directory.CreateDirectory(Path.Combine(_root, "dst")).FullName;
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private string Write(string dir, string name, string text, DateTime? time = null)
        {
            var path = Path.Combine(dir, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
            if (time != null) File.SetLastWriteTimeUtc(path, time.Value);
            return path;
        }

        private static readonly DateTime Old = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), New = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Copy_Folder_CopiesTree_ReportsBytes_AndKeepsTimestamps()
        {
            var a = Write(_src, "tree/a.txt", "12345", Old);
            Write(_src, "tree/sub/b.txt", "678");
            var reports = new List<TransferProgress>();
            var result = new TransferEngine(new TransferOptions(), progress: new SyncProgress(reports.Add)).Copy(new[] { Path.Combine(_src, "tree") }, _dst);

            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Copied, Is.EqualTo(2));
            Assert.That(File.ReadAllText(Path.Combine(_dst, "tree", "sub", "b.txt")), Is.EqualTo("678"));
            Assert.That(File.GetLastWriteTimeUtc(Path.Combine(_dst, "tree", "a.txt")), Is.EqualTo(Old), "Preserve timestamps");
            Assert.That(reports.Last().Bytes, Is.EqualTo(8));
            Assert.That(reports.Last().TotalBytes, Is.EqualTo(8));
            Assert.That(reports.Max(r => r.FileCount), Is.EqualTo(2));
            Assert.That(Directory.GetFiles(_dst, "*.mmc-part", SearchOption.AllDirectories), Is.Empty);
        }

        [TestCase(ConflictPolicy.Skip, "old", false)]
        [TestCase(ConflictPolicy.OverwriteOlder, "new", false)]
        [TestCase(ConflictPolicy.KeepBoth, "old", true)]
        public void Copy_Conflict_FollowsPolicy(ConflictPolicy policy, string expectedTarget, bool expectCopyNextToIt)
        {
            Write(_src, "a.txt", "new", New);
            Write(_dst, "a.txt", "old", Old);
            new TransferEngine(new TransferOptions { Policy = policy }).Copy(new[] { Path.Combine(_src, "a.txt") }, _dst);
            Assert.That(File.ReadAllText(Path.Combine(_dst, "a.txt")), Is.EqualTo(expectedTarget));
            Assert.That(File.Exists(Path.Combine(_dst, "a (1).txt")), Is.EqualTo(expectCopyNextToIt));
        }

        [Test]
        public void Copy_OverwriteOlder_KeepsNewerTarget()
        {
            Write(_src, "a.txt", "old", Old);
            Write(_dst, "a.txt", "new", New);
            var result = new TransferEngine(new TransferOptions { Policy = ConflictPolicy.OverwriteOlder }).Copy(new[] { Path.Combine(_src, "a.txt") }, _dst);
            Assert.That(File.ReadAllText(Path.Combine(_dst, "a.txt")), Is.EqualTo("new"));
            Assert.That(result.Skipped, Is.EqualTo(1));
        }

        [Test]
        public void Copy_Ask_ApplyToAll_AsksOnce()
        {
            foreach (var n in new[] { "a.txt", "b.txt", "c.txt" })
            {
                Write(_src, n, "src");
                Write(_dst, n, "dst");
            }
            int asked = 0;
            var engine = new TransferEngine(new TransferOptions(), c => { asked++; return new ConflictAnswer(ConflictDecision.Replace, applyToAll: true); });
            engine.Copy(Directory.GetFiles(_src), _dst);
            Assert.That(asked, Is.EqualTo(1));
            Assert.That(Directory.GetFiles(_dst).Select(File.ReadAllText), Is.All.EqualTo("src"));
        }

        [Test]
        public void Copy_AskCancel_StopsTheTransfer()
        {
            Write(_src, "a.txt", "src");
            Write(_dst, "a.txt", "dst");
            Write(_src, "b.txt", "src");
            var result = new TransferEngine(new TransferOptions(), _ => new ConflictAnswer(ConflictDecision.Cancel))
                .Copy(new[] { Path.Combine(_src, "a.txt"), Path.Combine(_src, "b.txt") }, _dst);
            Assert.That(result.Cancelled, Is.True);
            Assert.That(File.Exists(Path.Combine(_dst, "b.txt")), Is.False);
        }

        [Test]
        public void Copy_CancelledToken_CopiesNothing()
        {
            Write(_src, "a.txt", "src");
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var result = new TransferEngine(new TransferOptions(), token: cts.Token).Copy(new[] { Path.Combine(_src, "a.txt") }, _dst);
            Assert.That(result.Cancelled, Is.True);
            Assert.That(Directory.GetFiles(_dst), Is.Empty);
        }

        [Test]
        public void Copy_Paused_WaitsUntilResumed()
        {
            var big = Path.Combine(_src, "big.bin");
            File.WriteAllBytes(big, new byte[3 * 1024 * 1024]);
            var pause = new PauseGate();
            pause.Pause();
            var task = Task.Run(() => new TransferEngine(new TransferOptions(), pause: pause).Copy(new[] { big }, _dst));
            Assert.That(task.Wait(300), Is.False, "nothing finishes while paused");
            pause.Resume();
            Assert.That(task.Wait(5000), Is.True);
            Assert.That(new FileInfo(Path.Combine(_dst, "big.bin")).Length, Is.EqualTo(3 * 1024 * 1024));
        }

        [Test]
        public void Copy_Verify_PassesForGoodCopy()
        {
            Write(_src, "a.txt", "same bytes");
            var result = new TransferEngine(new TransferOptions { Verify = true }).Copy(new[] { Path.Combine(_src, "a.txt") }, _dst);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(TransferEngine.SameHash(Path.Combine(_src, "a.txt"), Path.Combine(_dst, "a.txt")), Is.True);
        }

        [Test]
        public void Copy_FolderIntoItself_IsRefused()
        {
            var tree = Directory.CreateDirectory(Path.Combine(_src, "tree")).FullName;
            var result = new TransferEngine(new TransferOptions()).Copy(new[] { tree }, tree);
            Assert.That(result.Errors.Single(), Does.Contain("into itself"));
        }

        [Test]
        public void Copy_Symlink_IsCopiedAsLink_UnlessFollowed()
        {
            var target = Write(_src, "real.txt", "data");
            var link = Path.Combine(_src, "tree", "link.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(link));
            try { File.CreateSymbolicLink(link, target); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Assert.Ignore("symlinks not allowed here"); }

            new TransferEngine(new TransferOptions()).Copy(new[] { Path.Combine(_src, "tree") }, _dst);
            Assert.That(new FileInfo(Path.Combine(_dst, "tree", "link.txt")).LinkTarget, Is.EqualTo(target));

            var followed = Directory.CreateDirectory(Path.Combine(_root, "followed")).FullName;
            new TransferEngine(new TransferOptions { FollowSymlinks = true }).Copy(new[] { Path.Combine(_src, "tree") }, followed);
            var copy = new FileInfo(Path.Combine(followed, "tree", "link.txt"));
            Assert.That(copy.LinkTarget, Is.Null);
            Assert.That(File.ReadAllText(copy.FullName), Is.EqualTo("data"));
        }

        [Test]
        public void Copy_Include_FiltersFilesInsideFolders_ButNotMarkedFiles()
        {
            Write(_src, "tree/a.txt", "a");
            Write(_src, "tree/b.md", "b");
            var md = Write(_src, "top.md", "t");
            new TransferEngine(new TransferOptions { Include = p => p.EndsWith(".txt") }).Copy(new[] { Path.Combine(_src, "tree"), md }, _dst);
            Assert.That(File.Exists(Path.Combine(_dst, "tree", "a.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_dst, "tree", "b.md")), Is.False);
            Assert.That(File.Exists(Path.Combine(_dst, "top.md")), Is.True, "a marked file is copied even if it doesn't match");
        }

        [Test]
        public void Move_OntoExistingFile_UsesPolicy_InsteadOfThrowing()
        {
            var a = Write(_src, "a.txt", "new");
            Write(_dst, "a.txt", "old");
            var result = new TransferEngine(new TransferOptions { Policy = ConflictPolicy.KeepBoth }).Move(new[] { a }, _dst);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(File.Exists(a), Is.False, "source removed after the copy");
            Assert.That(File.ReadAllText(Path.Combine(_dst, "a (1).txt")), Is.EqualTo("new"));
        }

        [Test]
        public void Move_SkippedConflict_KeepsSource()
        {
            var a = Write(_src, "a.txt", "new");
            Write(_dst, "a.txt", "old");
            new TransferEngine(new TransferOptions { Policy = ConflictPolicy.Skip }).Move(new[] { a }, _dst);
            Assert.That(File.Exists(a), Is.True);
        }

        [Test]
        public void Move_FreeName_RenamesFolder()
        {
            Write(_src, "tree/a.txt", "a");
            var result = new TransferEngine(new TransferOptions()).Move(new[] { Path.Combine(_src, "tree") }, _dst);
            Assert.That(result.Copied, Is.EqualTo(1));
            Assert.That(Directory.Exists(Path.Combine(_src, "tree")), Is.False);
            Assert.That(File.Exists(Path.Combine(_dst, "tree", "a.txt")), Is.True);
        }

        // Progress<T> posts to the thread pool; tests need the reports in order and before Copy returns.
        private sealed class SyncProgress : IProgress<TransferProgress>
        {
            private readonly Action<TransferProgress> _report;
            public SyncProgress(Action<TransferProgress> report) => _report = report;
            public void Report(TransferProgress value) => _report(value);
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class TrashTest
    {
        private string _root;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, "trash-" + Guid.NewGuid().ToString("N"))).FullName;
            Trash.MacTrashDir = Path.Combine(_root, ".Trash");
            Trash.LinuxTrashDir = Path.Combine(_root, "Trash");
        }

        [TearDown]
        public void After()
        {
            Trash.MacTrashDir = null;
            Trash.LinuxTrashDir = null;
            Directory.Delete(_root, true);
        }

        [Test]
        public void MoveToTrash_MovesItem_AndKeepsBothOnNameClash()
        {
            if (OperatingSystem.IsWindows()) Assert.Ignore("Recycle Bin is not redirected in tests");
            var first = Path.Combine(_root, "a.txt");
            File.WriteAllText(first, "1");
            Trash.MoveToTrash(first);
            File.WriteAllText(first, "2");
            Trash.MoveToTrash(first);

            Assert.That(File.Exists(first), Is.False);
            var files = OperatingSystem.IsMacOS() ? Trash.MacTrashDir : Path.Combine(Trash.LinuxTrashDir, "files");
            Assert.That(Directory.GetFiles(files).Select(Path.GetFileName), Is.EquivalentTo(new[] { "a.txt", "a (1).txt" }));
            Assert.That(Trash.Unavailable(Path.Combine(_root, "other.txt")), Is.Null);
        }
    }
}
