using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace IOLibTest
{
    [TestFixture]
    public class DirRenameTest
    {
        private readonly string OldDirectory = Path.Combine(TestPaths.Root, "test_dir");
        private readonly string NewDirectory = Path.Combine(TestPaths.Root, "myDir");

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
            ClassicAssert.IsTrue(Directory.Exists(NewDirectory));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteDirIfExist(OldDirectory);
            FileHelper.DeleteDirIfExist(NewDirectory);
        }
    }
}