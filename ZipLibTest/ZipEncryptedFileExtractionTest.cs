using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    [Ignore("doesn't work need to fix")]
    public class ZipEncryptedFileExtractionTest
    {
        private readonly string DestinationPath = Path.Combine(Path.GetTempPath(), "test_dir");
        private readonly string FilePath = Path.Combine(Path.GetTempPath(), "test_dir\\file");
        private const string Password = @"!Ma78";
        private readonly ZipAdapter _zipAdapter = new ZipAdapter();

        [SetUp]
        public void Before()
        {
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.KB);
            _zipAdapter.CompressFilesWithEncryption(new ZipParameters(DestinationPath + @"\file", Password), FilePath);
            FileHelper.DeleteIfExist(FilePath);
        }

        [Test]
        public void ExtractFile_Test()
        {
            //When
            _zipAdapter.UnCompressFile(new ZipParameters(DestinationPath + @"\file.zip", Password), DestinationPath);
            //Then
            Assert.IsTrue(File.Exists(FilePath));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteAllFilesInDirectory(DestinationPath);
        }
    }
}