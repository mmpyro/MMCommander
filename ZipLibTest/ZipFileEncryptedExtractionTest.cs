using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipFileEncryptedExtractionTest
    {
        private const string DestinationPath = @"D:\test";
        private const string FilePath = @"D:\test\file.txt";
        private readonly ZipAdapter _zipAdapter = new ZipAdapter();

        [SetUp]
        public void Before()
        {
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.KB);
            _zipAdapter.CompressFiles(new ZipParameters(DestinationPath + @"\file", "aa"), FilePath);
            FileHelper.DeleteIfExist(FilePath);
        }

        [Test]
        public void TEST()
        {
            //When
            _zipAdapter.UnCompressFile(new ZipParameters(DestinationPath + @"\file.zip", ""), DestinationPath);
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