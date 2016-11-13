using System;
using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipFileTest
    {
        private readonly string DestinationPath = Path.Combine(Path.GetTempPath(), "test_dir");
        private readonly string FilePath = Path.Combine(Path.GetTempPath(), "test_dir\\file");

        [SetUp]
        public void Before()
        {
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.KB);
        }

        [Test]
        public void CreteZipFromFile_Test()
        {
            //Given
            string filePath = FilePath;
            ZipAdapter zipAdapter = new ZipAdapter();
            string path = Path.Combine(DestinationPath, "file.zip");
            //When
            zipAdapter.CompressFiles(new ZipParameters(filePath, "", ZipType.Zip), FilePath);
            //Then
            Assert.True(File.Exists(path));
        }

        [TestCase("aa")]
        [TestCase("")]
        public void CreateZipWithEncryptionFile_Test(string password)
        {
            //Given
            string filePath = FilePath;
            ZipAdapter zipAdapter = new ZipAdapter();
            string path = Path.Combine(DestinationPath, "file.zip");
            //When
            zipAdapter.CompressFiles(new ZipParameters(filePath, password, ZipType.Zip), FilePath);
            //Then
            Assert.True(File.Exists(path));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteIfExist(Path.Combine(DestinationPath, "file.zip"));
            FileHelper.DeleteIfExist(FilePath);
        }
    }
}
