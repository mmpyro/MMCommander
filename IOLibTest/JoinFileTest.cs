using System.IO;
using System.Linq;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class JoinFileTest
    {
        private const string DestinationPath = @"D:\test";
        private const string FilePath = @"D:\test\file.txt";
        private IFileFactory _fileFactory;
            
        [SetUp]
        public void Before()
        {
            _fileFactory = new FileFactory();
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.B);
            var fileManager = new FileManager(_fileFactory);
            var file = _fileFactory.CreateFileMsg(FilePath);
            fileManager.Split(file, 100, FileSizeUnit.B, DestinationPath);
            File.Delete(FilePath);
        }

        [Test]
        public void JoinFile_Test()
        {
            //Given
            var fileManager = new FileManager(_fileFactory);
            //When
            fileManager.Join(DestinationPath,"file", FilePath);
            var directoryInfo = new DirectoryInfo(DestinationPath);
            var files = directoryInfo.GetFiles();
            //Then
            Assert.AreEqual(11, files.Length);
            Assert.AreEqual(1000,files.Max(t => t.Length));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteAllFilesInDirectory(DestinationPath);
        }
    }
}