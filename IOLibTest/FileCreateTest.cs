using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{

    [TestFixture]
    public class FileCreateTest
    {
        private const string Path = @"D:\test\file.dat";

        [SetUp]
        public void Before()
        {
            FileHelper.DeleteIfExist(Path);
        }

        [Test]
        public void CreateEmptyFile_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            IFileManager manager = new FileManager(fileFactory);
            //When
            manager.CreateFile(Path);
            //Then
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteIfExist(Path);
        }
    }
}