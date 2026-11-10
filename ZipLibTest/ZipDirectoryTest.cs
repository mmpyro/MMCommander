using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipDirectoryTest
    {
        private readonly string RootDirectoryPath = Path.Combine(TestPaths.Root, "test_dir/dir");
        private readonly string SubDirectoryPath = Path.Combine(TestPaths.Root, "test_dir/dir/sub");
        private readonly string RepositoryPath = Path.Combine(TestPaths.Root, "test_dir/dir/zippedDir");

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
            zipAdapter.CompressDir(new ZipParameters(RepositoryPath, "", ZipType.Zip), RootDirectoryPath );
            //Then
            ClassicAssert.IsTrue(File.Exists(RepositoryPath+@".zip"));
        }

        [TestCase("")]
        [TestCase("aa")]
        [TestCase(@"!Ma78")]
        public void CreateZipFromDirectoryWithEncryption_Test(string password)
        {
            //Given
            ZipAdapter zipAdapter = new ZipAdapter();
            //When
            zipAdapter.CompressDirWithEncryption(new ZipParameters(RepositoryPath, password, ZipType.Zip), RootDirectoryPath);
            //Then
            ClassicAssert.IsTrue(File.Exists(RepositoryPath + @".zip"));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(RootDirectoryPath);
            FileHelper.DeleteIfExist(RepositoryPath + @".zip");
        }
    }
}