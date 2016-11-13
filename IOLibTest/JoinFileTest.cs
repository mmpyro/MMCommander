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
        private readonly string DestinationPath = Path.Combine(Path.GetTempPath(), "test_dir");
        private readonly string FilePath = Path.Combine(Path.GetTempPath(), "test_dir\\file.txt");
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