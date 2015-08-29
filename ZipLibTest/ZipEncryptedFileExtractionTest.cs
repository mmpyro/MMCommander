using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipEncryptedFileExtractionTest
    {
        private const string DestinationPath = @"D:\test";
        private const string FilePath = @"D:\test\file.txt";
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