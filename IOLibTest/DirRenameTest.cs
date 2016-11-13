using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class DirRenameTest
    {
        private readonly string OldDirectory = Path.Combine(Path.GetTempPath(), "test_dir");
        private readonly string NewDirectory = Path.Combine(Path.GetTempPath(), "myDir");

        [SetUp]
        public void Before()
        {
            FileHelper.DeleteDirIfExist(NewDirectory);
            FileHelper.CreateDirIfNotExist(OldDirectory);
        }

        [Test]
        public void DirectoryRename_Test()
        {
            //Given
            var fileFactory = new FileFactory();
            var dir = fileFactory.CreateFileMsg(OldDirectory);
            //When
            dir.Rename("myDir");
            //Then
            Assert.IsTrue(Directory.Exists(NewDirectory));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(OldDirectory);
            FileHelper.DeleteDirIfExist(NewDirectory);
        }
    }
}