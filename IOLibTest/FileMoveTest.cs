using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class FileMoveTest
    {
        private readonly string DestinationPath = Path.Combine(Path.GetTempPath(), "test_dir");

        [SetUp]
        public void Before()
        {
            FileHelper.InitCopyFileTest(DestinationPath);
        }

        [Test]
        public void FileMove_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            string dst1 = Path.Combine(FileHelper.Dir1(), FileHelper.FileName());
            string dst2 = Path.Combine(FileHelper.Dir2(), FileHelper.FileName());
            FileStructure file = (FileStructure) fileFactory.CreateFileMsg(Path.Combine(DestinationPath, dst1));
            var dir = fileFactory.CreateFileMsg(Path.Combine(DestinationPath, FileHelper.Dir2()));
            //When
            file.Move(dir);
            //Then
            string destinationFilePath = Path.Combine(DestinationPath, dst2);
            Assert.IsTrue(FileHelper.Exist(destinationFilePath));
            Assert.That(file.FullName, Is.EqualTo(destinationFilePath));
        }

        [TearDown]
        public void After()
        {
            FileHelper.CleanCopyFileTest(DestinationPath);
        }
    }
}