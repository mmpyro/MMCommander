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
        private const string DestinationPath = @"D:\test";
        private const string FilePath = @"D:\test\file.txt";

        [SetUp]
        public void Before()
        {
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.KB);
        }

        [TestCase(@"D:\test\file")]
        public void CreteZipFromFile_Test(string filePath)
        {
            //Given
            ZipAdapter zipAdapter = new ZipAdapter();
            string path = Path.Combine(DestinationPath, "file.zip");
            //When
            zipAdapter.CompressFiles(new ZipParameters(filePath, "", ZipType.Zip), FilePath);
            //Then
            Assert.True(File.Exists(path));
        }

        [TestCase(@"D:\test\file", "aa")]
        [TestCase(@"D:\test\file", "")]
        public void CreateZipWithEncryptionFile_Test(string filePath, string password)
        {
            //Given
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
