using System;
using System.IO;
using IOLib;
using Moq;
using NUnit.Framework;

namespace IOLibTest
{
    internal class FakeFile : IAbstractFileStructure
    {
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string Ext { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastModifyTime { get; private set; }
        public bool IsDirectory { get; private set; }
        public long Size { get; private set; }
        public bool IsReadOnly { get; private set; }
        public FileAttributes Attributes { get; set; }

        public FileType Type => FileType.Normal;

        public void Delete()
        {
        }

        public void Move(IAbstractFileStructure destinationDirectory)
        {
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string, bool> allowOverride)
        {
        }

        public void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
        }

        public void Rename(string newName)
        {
        }

        public void SetSize(int size)
        {
            Size = size;
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string, string, bool> allowOverride)
        {
            
        }
    }

    [TestFixture]
    public class Split_Test
    {
         
        [Test]
        public void KB_UnitConvertTest()
        {
            //Given
            int size = 1;
            //When
            int result = size*(int) FileSizeUnit.KB;
            //Then
            Assert.AreEqual(1024 ,result);
        }

        [Test]
        public void MB_UnitConvertTest()
        {
            //Given
            int size = 1;
            //When
            int result = size * (int)FileSizeUnit.MB;
            //Then
            Assert.AreEqual(1024*1024, result);
        }

        [TestCase(101)]
        [TestCase(100)]
        [ExpectedException(typeof(ArgumentException))]
        public void SplitVerifySize_Test(int size)
        {
            //Given
            var fileFactory = new FileFactory();
            var fileManager = new FileManager(fileFactory);
            IAbstractFileStructure file = new FakeFile();
            ((FakeFile) file).SetSize(100);
            //When
            fileManager.Split( file, size, FileSizeUnit.B,string.Empty);
            //Then
            
        }
    }
}