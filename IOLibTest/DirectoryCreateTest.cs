using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using System.IO;

namespace IOLibTest
{
    [TestFixture]
    public class DirectoryCreateTest
    {
        private readonly string DirectoryPath = Path.Combine(Path.GetTempPath(), "test_dir");

        [SetUp]
        public void Before()
        {
            FileHelper.DeleteDirIfExist(DirectoryPath);
        }

        [Test]
        public void CreateDirectory_Test()
        {
            //Given
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            //When
            manager.CreateDirectory(DirectoryPath);
            //Then
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(DirectoryPath);
        }
    }
}