using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using System.IO;

namespace IOLibTest
{

    [TestFixture]
    public class FileCreateTest
    {
        private readonly string filePath = Path.Combine(Path.GetTempPath(), "file.dat");

        [SetUp]
        public void Before()
        {
            FileHelper.DeleteIfExist(filePath);
        }

        [Test]
        public void CreateEmptyFile_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            IFileManager manager = new FileManager(fileFactory);
            //When
            manager.CreateFile(filePath);
            //Then
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteIfExist(filePath);
        }
    }
}