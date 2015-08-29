using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class FileRenameTest
    {
        private const string DestinationPath = @"D:\test";
        private const string OldName = "file.txt";
        private const string NewName = "myFile.dat";

        [SetUp]
        public void Before()
        {
            FileHelper.CreateIfNotExist(Path.Combine(DestinationPath,OldName));
        }

        [Test]
        public void FileRename_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            var file = fileFactory.CreateFileMsg(Path.Combine(DestinationPath, OldName));
            //When
            file.Rename(NewName);
            //Then
            string newNameFullPath = Path.Combine(DestinationPath, NewName);
            Assert.IsTrue(File.Exists(newNameFullPath));
            Assert.That(file.FullName, Is.EqualTo(newNameFullPath));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteAllFilesInDirectory(DestinationPath);
        }
    }
}