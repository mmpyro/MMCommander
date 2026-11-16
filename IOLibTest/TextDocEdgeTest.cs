using System;
using System.IO;
using System.Linq;
using System.Text;
using IOLib;
using System.Runtime.Versioning;
using NUnit.Framework;

namespace IOLibTest
{
    // Save safety and the text/binary decision of TextDoc (compare files window).
    [TestFixture]
    public class TextDocEdgeTest
    {
        private string _root;

        [SetUp]
        public void Before() => _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, "textdoc-" + Guid.NewGuid().ToString("N"))).FullName;

        [TearDown]
        public void After()
        {
            if (!OperatingSystem.IsWindows())
                foreach (var dir in Directory.GetDirectories(_root, "*", SearchOption.AllDirectories).Append(_root))
                    File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Directory.Delete(_root, true);
        }

        private string Put(string name, byte[] bytes)
        {
            var path = Path.Combine(_root, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        // T26: a failed save must keep the file and leave no temp file behind.
        [Test, Platform(Exclude = "Win"), UnsupportedOSPlatform("windows")]
        public void Save_FolderNotWritable_Throws_AndLeavesTheFileAlone()
        {
            var dir = Directory.CreateDirectory(Path.Combine(_root, "locked")).FullName;
            var path = Path.Combine(dir, "a.txt");
            File.WriteAllText(path, "old\n");
            var doc = TextDoc.Load(path);
            doc.Lines[0] = "new";
            File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

            Assert.That(() => doc.Save(), Throws.InstanceOf<UnauthorizedAccessException>().Or.InstanceOf<IOException>());
            Assert.That(File.ReadAllText(path), Is.EqualTo("old\n"));
            Assert.That(Directory.GetFiles(dir), Is.EqualTo(new[] { path }), "no temp file left");
        }

        // T26b [assumption]: like other editors, a read-only file is not silently replaced.
        [Test, Platform(Exclude = "Win"), UnsupportedOSPlatform("windows")]
        public void Save_ReadOnlyFile_IsNotReplaced()
        {
            var path = Put("ro.txt", Encoding.UTF8.GetBytes("old\n"));
            var doc = TextDoc.Load(path);
            doc.Lines[0] = "new";
            File.SetUnixFileMode(path, UnixFileMode.UserRead);

            Assert.That(() => doc.Save(), Throws.InstanceOf<UnauthorizedAccessException>().Or.InstanceOf<IOException>());
            Assert.That(File.ReadAllText(path), Is.EqualTo("old\n"));
        }

        // T27: saving through a symlink changes the target and keeps the link.
        [Test, Platform(Exclude = "Win"), UnsupportedOSPlatform("windows")]
        public void Save_ThroughSymlink_KeepsTheLink()
        {
            var target = Put("target.txt", Encoding.UTF8.GetBytes("old\n"));
            var link = Path.Combine(_root, "link.txt");
            File.CreateSymbolicLink(link, target);
            var doc = TextDoc.Load(link);
            doc.Lines[0] = "new";
            doc.Save();

            Assert.That(new FileInfo(link).LinkTarget, Is.EqualTo(target), "still a symlink");
            Assert.That(File.ReadAllText(target), Is.EqualTo("new\n"));
        }

        // T28: an executable script stays executable.
        [Test, Platform(Exclude = "Win"), UnsupportedOSPlatform("windows")]
        public void Save_KeepsTheFileMode()
        {
            var path = Put("run.sh", Encoding.UTF8.GetBytes("echo a\n"));
            var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(path, mode);
            var doc = TextDoc.Load(path);
            doc.Lines[0] = "echo b";
            doc.Save();
            Assert.That(File.GetUnixFileMode(path), Is.EqualTo(mode));
        }

        // T29
        [Test]
        public void Load_Utf16BigEndian_RoundTrips()
        {
            var bytes = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes("a\r\nb")).ToArray();
            var path = Put("be.txt", bytes);
            var doc = TextDoc.Load(path);
            Assert.That(doc.Lines, Is.EqualTo(new[] { "a", "b" }));
            doc.Save();
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
        }

        // T29: UTF-32 LE starts with the UTF-16 LE BOM; it must not open as text full of NULs.
        [TestCase(false, TestName = "Load_Utf32_IsNotShownAsNulText")]
        [TestCase(true, TestName = "Load_Utf32BigEndian_RoundTrips")]
        public void Load_Utf32(bool bigEndian)
        {
            var bom = bigEndian ? new byte[] { 0, 0, 0xFE, 0xFF } : new byte[] { 0xFF, 0xFE, 0, 0 };
            var bytes = bom.Concat(new UTF32Encoding(bigEndian, false).GetBytes("ab\n")).ToArray();
            var path = Put("u32.txt", bytes);
            var doc = TextDoc.Load(path);
            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Lines, Is.EqualTo(new[] { "ab" }));
            doc.Save();
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
        }

        // Bug 4: an edit keeps the file's final-newline state.
        [TestCase("a\nb", "a\nb\nc")]
        [TestCase("a\n", "a\nc\n")]
        public void Replace_KeepsTheFinalNewlineState(string before, string expected)
        {
            var path = Put("end.txt", Encoding.UTF8.GetBytes(before));
            var doc = TextDoc.Load(path);
            doc.Replace(doc.Lines.Count, 0, new[] { "c" });
            doc.Save();
            Assert.That(File.ReadAllText(path), Is.EqualTo(expected));
        }

        // T29: over the 20 MB text limit → binary (null).
        [Test]
        public void Load_Over20MB_IsBinary()
        {
            var path = Path.Combine(_root, "big.txt");
            using (var f = File.Create(path))
            {
                var line = Encoding.ASCII.GetBytes(new string('x', 1023) + "\n");
                for (int i = 0; i < 20 * 1024 + 1; i++) f.Write(line);
            }
            Assert.That(TextDoc.Load(path), Is.Null);
        }

        // T29: only the first 8 KB is checked for NUL; a later NUL still round-trips as text.
        [Test]
        public void Load_NulAfter8KB_StillSavesTheSameBytes()
        {
            var bytes = Encoding.ASCII.GetBytes(new string('a', 9000) + "\n").Concat(new byte[] { 0, (byte)'\n' }).ToArray();
            var path = Put("late-nul.txt", bytes);
            var doc = TextDoc.Load(path);
            Assert.That(doc, Is.Not.Null);
            doc.Save();
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
        }
    }
}
