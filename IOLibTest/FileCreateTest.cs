using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using System.IO;

namespace IOLibTest
{

    [TestFixture]
    public class FileCreateTest
    {
        private readonly string filePath = Path.Combine(TestPaths.Root, "file.dat");

        [SetUp]
        public void Before()
        {
            FileHelper.DeleteIfExist(filePath);
        }

        [Test]
        public void CreateEmptyFile_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            IFileManager manager = new FileManager(fileFactory);
            //When
            manager.CreateFile(filePath);
            //Then
        }

        // "New file" with an existing name used to empty that file.
        [Test]
        public void CreateFile_Existing_ThrowsAndKeepsContent()
        {
            File.WriteAllText(filePath, "data");
            IFileManager manager = new FileManager(new FileFactory());

            Assert.Throws<IOException>(() => manager.CreateFile(filePath));
            Assert.That(File.ReadAllText(filePath), Is.EqualTo("data"));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteIfExist(filePath);
        }
    }
}