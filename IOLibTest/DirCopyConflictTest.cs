using System;
using System.IO;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class DirCopyConflictTest
    {
        private string _root, _source, _target;
        private readonly FileFactory _factory = new FileFactory();

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, Guid.NewGuid().ToString("N"))).FullName;
            _source = Directory.CreateDirectory(Path.Combine(_root, "src", "docs", "sub")).Parent.FullName;
            File.WriteAllText(Path.Combine(_source, "report.txt"), "new");
            File.WriteAllText(Path.Combine(_source, "sub", "deep.txt"), "new");
            File.WriteAllText(Path.Combine(_source, "sub", "Makefile"), "no extension");
            _target = Directory.CreateDirectory(Path.Combine(_root, "dst", "docs", "sub")).Parent.Parent.FullName;
            File.WriteAllText(Path.Combine(_target, "docs", "report.txt"), "old");
            File.WriteAllText(Path.Combine(_target, "docs", "sub", "deep.txt"), "old");
        }

        [TearDown]
        public void After() => _factory.CreateFileMsg(_root).Delete();

        private string Read(params string[] parts) => File.ReadAllText(Path.Combine(_target, Path.Combine(parts)));

        // BUG-01: nested files used to be overwritten without asking.
        [Test]
        public void Copy_AnsweredNo_KeepsNestedTargets()
        {
            var asked = 0;
            _factory.CreateFileMsg(_source).Copy(_factory.CreateFileMsg(_target), (_, _) => { asked++; return false; });
            Assert.That(asked, Is.EqualTo(2));
            Assert.That(Read("docs", "report.txt"), Is.EqualTo("old"));
            Assert.That(Read("docs", "sub", "deep.txt"), Is.EqualTo("old"));
        }

        [Test]
        public void Copy_AnsweredYes_ReplacesNestedTargets()
        {
            _factory.CreateFileMsg(_source).Copy(_factory.CreateFileMsg(_target), (_, _) => true);
            Assert.That(Read("docs", "report.txt"), Is.EqualTo("new"));
            Assert.That(Read("docs", "sub", "deep.txt"), Is.EqualTo("new"));
            Assert.That(Read("docs", "sub", "Makefile"), Is.EqualTo("no extension"));
        }

        // BUG-16: copying a folder into itself used to recurse until PathTooLongException.
        [TestCase("")]
        [TestCase("sub")]
        public void Copy_IntoItself_Throws(string child)
        {
            var destination = _factory.CreateFileMsg(Path.Combine(_source, child));
            Assert.Throws<IOException>(() => _factory.CreateFileMsg(_source).Copy(destination, (_, _) => true));
            Assert.That(Directory.Exists(Path.Combine(_source, "sub", "docs")), Is.False);
        }

        [Test]
        public void Copy_IntoSiblingWithSamePrefix_Works()
        {
            var sibling = Directory.CreateDirectory(_source + "2").FullName;
            _factory.CreateFileMsg(_source).Copy(_factory.CreateFileMsg(sibling), (_, _) => true);
            Assert.That(File.Exists(Path.Combine(sibling, "docs", "report.txt")), Is.True);
        }

        // BUG-30: a folder copy used to strip ReadOnly from the source files.
        [Test]
        public void Copy_KeepsReadOnlyOnSourceAndCopy()
        {
            var file = Path.Combine(_source, "report.txt");
            File.SetAttributes(file, FileAttributes.ReadOnly);
            var empty = Directory.CreateDirectory(Path.Combine(_root, "empty")).FullName;
            _factory.CreateFileMsg(_source).Copy(_factory.CreateFileMsg(empty), (_, _) => true);
            Assert.That(new FileInfo(file).IsReadOnly, Is.True);
            Assert.That(new FileInfo(Path.Combine(empty, "docs", "report.txt")).IsReadOnly, Is.True);
        }
    }
}
