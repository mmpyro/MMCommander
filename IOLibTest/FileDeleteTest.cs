using System;
using System.Diagnostics;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class FileDeleteTest
    {
        private const string Path = @"D:\test\file.dat";

        [SetUp]
        public void Before()
        {
            FileHelper.CreateIfNotExist(Path);
        }

        [Test]
        public void DeleteFile_Test()
        {
            //Given
            IFileFactory fileFactory = new FileFactory();
            var file = fileFactory.CreateFileMsg(Path);
            //When
            file.Delete();
            //Then
            Debug.WriteLine(file.FullName);
            Assert.IsFalse(FileHelper.Exist(Path));
        }

        [TearDown]
        public void After()
        {
            FileHelper.DeleteIfExist(Path);
        }
    }
}