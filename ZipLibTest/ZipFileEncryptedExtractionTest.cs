using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;
using System.Threading;
using System;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipFileEncryptedExtractionTest
    {
        private readonly string DestinationPath = Path.Combine(Path.GetTempPath(), "test_dir");
        private readonly string FilePath = Path.Combine(Path.GetTempPath(), "test_dir\\file");
        private readonly ZipAdapter _zipAdapter = new ZipAdapter();

        [SetUp]
        public void Before()
        {
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.KB);
            _zipAdapter.CompressFiles(new ZipParameters(DestinationPath + @"\file", "aa"), FilePath);
            FileHelper.DeleteIfExist(FilePath);
        }

        [Test]
        public void ExtractFile_Test()
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