using System;
using System.IO;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class FileStructureTest
    {
        private string _root;
        private readonly FileFactory _factory = new FileFactory();

        [SetUp]
        public void Before() => _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, Guid.NewGuid().ToString("N"))).FullName;

        [TearDown]
        public void After() => _factory.CreateFileMsg(_root).Delete();

        // The factory used to decide by extension: "Makefile" became a folder, "v1.2" a file.
        [TestCase("a.txt", false)]
        [TestCase("Makefile", false)]
        [TestCase("dir", true)]
        [TestCase("release.v1.2", true)]
        public void Factory_ClassifiesByWhatThePathIs(string name, bool isDirectory)
        {
            var path = Path.Combine(_root, name);
            if (isDirectory)
                Directory.CreateDirectory(path);
            else
                File.WriteAllText(path, "x");
            Assert.That(_factory.CreateFileMsg(path).IsDirectory, Is.EqualTo(isDirectory));
        }

        // BUG-28: Equals ignored case but GetHashCode did not.
        [Test]
        public void EqualFiles_HaveEqualHashCodes()
        {
            var lower = new FileStructure("/tmp/Some/File.txt");
            var upper = new FileStructure("/tmp/SOME/file.TXT");
            Assert.That(lower, Is.EqualTo(upper));
            Assert.That(lower.GetHashCode(), Is.EqualTo(upper.GetHashCode()));
        }
    }
}
