using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class DeleteDirectoryTest
    {
        private readonly string DirectoryPath = Path.Combine(Path.GetTempPath(), "test_dir");

        [SetUp]
        public void Before()
        {
            FileHelper.CreateDirIfNotExist(DirectoryPath);
        }

        [Test]
        public void DeleteDirectory_Test()
        {
            //Given
            var fileFactory = new FileFactory();
            var dir = fileFactory.CreateFileMsg(DirectoryPath);
            //When
            dir.Delete();
            //Then
            Assert.IsFalse(Directory.Exists(DirectoryPath));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(DirectoryPath);
        }
    }
}