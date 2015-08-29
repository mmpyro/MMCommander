using System.IO;
using System.Linq;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class FileSplitTest
    {
        private const string DestinationPath = @"D:\test";
        private const string FilePath = @"D:\test\file.txt";

        [SetUp]
        public void Before()
        {
            FileHelper.CreateFileWithCertainSize(FilePath, 1000, FileSizeUnit.B);
        } 

        [Test]
        public void SplitFile_Test()
        {
            //Given
            var fileFactory = new FileFactory();
            var fileManager = new FileManager(fileFactory);
            var file = fileFactory.CreateFileMsg(FilePath);
            //When
            fileManager.Split(file, 100,FileSizeUnit.B, DestinationPath);
            File.Delete(FilePath);
            var directoryInfo = new DirectoryInfo(DestinationPath);
            var files = directoryInfo.GetFiles().Where(t => t.Name.Contains("file")).ToArray();
            //Then
            Assert.AreEqual(10, files.Length);
            Assert.AreEqual(100,files[0].Length);
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteAllFilesInDirectory(DestinationPath);
        }   
    }
}