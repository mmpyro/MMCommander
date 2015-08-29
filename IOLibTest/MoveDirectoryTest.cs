using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class MoveDirectoryTest
    {
        private const string DirectoryPath = @"D:\test";

        [SetUp]
        public void Before()
        {
            FileHelper.CreateDirIfNotExist(Path.Combine(DirectoryPath, "dir1"));
            FileHelper.CreateDirIfNotExist(Path.Combine(DirectoryPath, "dir2"));
            FileHelper.CreateIfNotExist(Path.Combine(Path.Combine(DirectoryPath, "dir1"),"file.txt"));
        }

        [Test]
        public void MoveDirectory_Test()
        {
            //Given
            var fileFactory = new FileFactory();
            var dir1 = fileFactory.CreateFileMsg(Path.Combine(DirectoryPath, "dir1"));
            var dir2 = fileFactory.CreateFileMsg(Path.Combine(DirectoryPath, "dir2"));
            //When
            dir1.Move(dir2);
            //Then
            Assert.IsTrue(Directory.Exists(Path.Combine(Path.Combine(DirectoryPath, "dir2"), "dir1")));
            Assert.IsTrue(File.Exists(Path.Combine(Path.Combine(DirectoryPath, "dir2"), "dir1\\file.txt")));
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