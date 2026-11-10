using System;
using System.Diagnostics;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.IO;

namespace IOLibTest
{
    [TestFixture]
    public class FileDeleteTest
    {
        private readonly string filePath = Path.Combine(TestPaths.Root, "file.dat");

        [SetUp]
        public void Before()
        {
            FileHelper.CreateIfNotExist(filePath);
        }

        [Test]
        public void DeleteFile_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            var file = fileFactory.CreateFileMsg(filePath);
            //When
            file.Delete();
            //Then
            Debug.WriteLine(file.FullName);
            ClassicAssert.IsFalse(FileHelper.Exist(filePath));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteIfExist(filePath);
        }
    }
}