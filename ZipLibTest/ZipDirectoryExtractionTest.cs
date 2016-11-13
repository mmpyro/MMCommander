using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipDirectoryExtractionTest
    {
        private readonly string DestinationPath = Path.Combine(Path.GetTempPath(), "test_dir");
        private readonly string RootDirectoryPath = Path.Combine(Path.GetTempPath(), "test_dir\\dir");
        private readonly string SubDirectoryPath = Path.Combine(Path.GetTempPath(), "test_dir\\dir\\sub");
        private readonly string RepositoryPath = Path.Combine(Path.GetTempPath(), "test_dir\\dir\\zippedDir");
        private readonly ZipAdapter _zipAdapter = new ZipAdapter();

        [SetUp]
        public void Before()
        {
            FileHelper.CreateDirIfNotExist(RootDirectoryPath);
            FileHelper.CreateDirIfNotExist(SubDirectoryPath);
            FileHelper.CreateFileWithCertainSize(Path.Combine(RootDirectoryPath, "file1"), 100, FileSizeUnit.KB);
            FileHelper.CreateFileWithCertainSize(Path.Combine(SubDirectoryPath, "file2"), 100, FileSizeUnit.KB);
            _zipAdapter.CompressDir(new ZipParameters(DestinationPath+@"\file","", ZipType.SevenZip), RootDirectoryPath );
            FileHelper.DeleteDirIfExist(RootDirectoryPath);
        }

        [Test]
        public void ExtractDir_Test()
        {
            //Given
            string extractedDirPath = Path.Combine(Path.GetTempPath(), "test_dir\\sub");
            //When
            _zipAdapter.UnCompressFile(new ZipParameters(DestinationPath + @"\file.7z", ""), DestinationPath);
            //Then
            Assert.IsTrue(Directory.Exists(extractedDirPath));
            Assert.IsTrue(File.Exists(Path.Combine(DestinationPath, "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(extractedDirPath, "file2")));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteAllFilesInDirectory(DestinationPath);
            FileHelper.DeleteDirIfExist(RootDirectoryPath);
            FileHelper.DeleteIfExist(RepositoryPath + @".7z");
        }
    }
}