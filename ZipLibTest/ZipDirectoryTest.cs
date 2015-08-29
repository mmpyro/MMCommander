using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipDirectoryTest
    {
        private const string RootDirectoryPath = @"D:\test\dir";
        private const string SubDirectoryPath = @"D:\test\dir\sub";
        private const string RepositoryPath = @"D:\test\dir\zippedDir";

        [SetUp]
        public void Before()
        {
            FileHelper.CreateDirIfNotExist(RootDirectoryPath);
            FileHelper.CreateDirIfNotExist(SubDirectoryPath);
            FileHelper.CreateFileWithCertainSize(Path.Combine(RootDirectoryPath, "file1"), 100, FileSizeUnit.KB);
            FileHelper.CreateFileWithCertainSize(Path.Combine(SubDirectoryPath, "file2"), 100, FileSizeUnit.KB);
        }

        [Test]
        public void CreateZipFromDirectory_Test()
        {
            //Given
            ZipAdapter zipAdapter = new ZipAdapter();
            //When
            zipAdapter.CompressDir(new ZipParameters(RepositoryPath, "", ZipType.SevenZip), RootDirectoryPath );
            //Then
            Assert.IsTrue(File.Exists(RepositoryPath+@".7z"));
        }

        [TestCase("")]
        [TestCase("aa")]
        [TestCase(@"!Ma78")]
        public void CreateZipFromDirectoryWithEncryption_Test(string password)
        {
            //Given
            ZipAdapter zipAdapter = new ZipAdapter();
            //When
            zipAdapter.CompressDir(new ZipParameters(RepositoryPath, "", ZipType.SevenZip), RootDirectoryPath);
            //Then
            Assert.IsTrue(File.Exists(RepositoryPath + @".7z"));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(RootDirectoryPath);
            FileHelper.DeleteIfExist(RepositoryPath + @".7z");
        }
    }
}