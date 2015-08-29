using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ZipFileExtractionTest
    {
        private const string DestinationPath = @"D:\test";
        private const string FilePath = @"D:\test.7z";
        private readonly ZipAdapter _zipAdapter = new ZipAdapter();

        [SetUp]
        public void Before()
        {
            File.Copy( FilePath, Path.Combine(DestinationPath, "test.7z"));
        }
 
        [Test]
        public void ExtractFile_Test()
        {
            //When
            _zipAdapter.UnCompressFile(new ZipParameters(DestinationPath + @"\test.7z", ""), DestinationPath);
            //Then
            Assert.IsTrue(Directory.Exists(Path.Combine(DestinationPath, "test")));
            Assert.IsTrue(Directory.Exists(Path.Combine(DestinationPath, @"test\dir")));
            Assert.IsTrue(File.Exists(Path.Combine(DestinationPath, @"test\aaa")));
            Assert.IsTrue(Directory.Exists(Path.Combine(DestinationPath, @"test\dir\dir2")));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteAllFilesInDirectory(DestinationPath);
            FileHelper.DeleteDirIfExist(Path.Combine(DestinationPath,"test"));
        }
    }
}