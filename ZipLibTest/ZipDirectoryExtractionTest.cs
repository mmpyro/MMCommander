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
        private const string DestinationPath = @"D:\test";
        private const string RootDirectoryPath = @"D:\test\dir";
        private const string SubDirectoryPath = @"D:\test\dir\sub";
        private const string RepositoryPath = @"D:\test\dir\zippedDir";
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

        [TestCase(@"D:\test\sub")]
        public void ExtractDir_Test(string extractedPath)
        {
            //When
            _zipAdapter.UnCompressFile(new ZipParameters(DestinationPath + @"\file.7z", ""), DestinationPath);
            //Then
            Assert.IsTrue(Directory.Exists(extractedPath));
            Assert.IsTrue(File.Exists(Path.Combine(DestinationPath, "file1")));
            Assert.IsTrue(File.Exists(Path.Combine(extractedPath, "file2")));
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