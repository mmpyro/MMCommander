using System;
using System.IO;
using Comander.CommanderIO;
using Comander.ViewModel;
using NUnit.Framework;
using File = System.IO.File;

namespace ComanderTest
{
    [TestFixture]
    public class InfoVMTest
    {
        private string _dir;

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(_dir = Path.Combine(Path.GetTempPath(), "InfoVMTest-" + Guid.NewGuid()));

        [TearDown]
        public void TearDown()
        {
            foreach (var f in Directory.GetFiles(_dir))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(_dir, true);
        }

        private string NewFile(string name)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllText(path, "x");
            return path;
        }

        private static InfoVM Info(string path) => new InfoVM(new FileMetaDataStructure(path));

        [TestCase((FileAttributes)0, "None")]
        [TestCase(FileAttributes.Normal, "None")]
        [TestCase(FileAttributes.Directory, "None")]
        [TestCase(FileAttributes.Hidden | FileAttributes.Normal, "Hidden")]
        [TestCase(FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.Archive, "Read-only, Hidden, Archive")]
        [TestCase(FileAttributes.ReparsePoint | FileAttributes.Directory, "Symbolic link")]
        public void Describe_UsesWords(FileAttributes attributes, string expected) =>
            Assert.That(InfoVM.Describe(attributes), Is.EqualTo(expected));

        [Test]
        public void ReadOnlyFile_ShowsReadOnly()
        {
            var path = NewFile("locked.txt");
            File.SetAttributes(path, FileAttributes.ReadOnly);
            Assert.That(Info(path).AttributesText, Does.Contain("Read-only"));
        }

        [Test]
        public void Dotfile_ShowsHidden()
        {
            if (OperatingSystem.IsWindows()) Assert.Ignore("Dotfiles are hidden only on macOS/Linux.");
            Assert.That(Info(NewFile(".secret")).AttributesText, Is.EqualTo("Hidden"));
        }

        [Test]
        public void Permissions_ShowRwxAndOctal()
        {
            var path = NewFile("plain.txt");
            if (OperatingSystem.IsWindows())
            {
                Assert.That(Info(path).Permissions, Is.Null);
                return;
            }
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            Assert.That(Info(path).Permissions, Is.EqualTo("rw-r--r--  (644)"));
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute);
            Assert.That(Info(path).Permissions, Is.EqualTo("rwxr-x---  (750)"));
        }
    }
}
