using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace IOLibTest
{
    [TestFixture]
    public class CopyDirectoryTest
    {
        private readonly string DirectoryPath = Path.Combine(TestPaths.Root, "test_dir");

        [SetUp]
        public void Before()
        {
            FileHelper.CreateDirIfNotExist(DirectoryPath);
            FileHelper.CreateDirIfNotExist(Path.Combine(DirectoryPath, "dir1"));
            FileHelper.CreateDirIfNotExist(Path.Combine(DirectoryPath, "dir2"));
            FileHelper.CreateIfNotExist(Path.Combine(Path.Combine(DirectoryPath, "dir1"), "file.txt"));
        }

        [Test]
        public void CopyDirectory_Test()
        {
            //Given
            var fileFactory = new FileFactory();
            var dir1 = fileFactory.CreateFileMsg(Path.Combine(DirectoryPath, "dir1"));
            var dir2 = fileFactory.CreateFileMsg(Path.Combine(DirectoryPath, "dir2"));
            //When
            dir1.OverrideCopy(dir2);
            //Then
            ClassicAssert.IsTrue(Directory.Exists(Path.Combine(Path.Combine(DirectoryPath, "dir2"), "dir1")));
            ClassicAssert.IsTrue(File.Exists(Path.Combine(Path.Combine(DirectoryPath, "dir2"), "dir1/file.txt")));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(Path.Combine(DirectoryPath, "dir1"));
            FileHelper.DeleteDirIfExist(Path.Combine(DirectoryPath, "dir2"));
            FileHelper.DeleteIfExist(Path.Combine(Path.Combine(DirectoryPath, "dir1"), "file.txt"));
        }
    }
}