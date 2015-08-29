using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using IOLib;
using Moq;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class FileSearchTest
    {

        [Test]
        public void FileSearchByName_Test()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            manager.SearchFiles( new SearchParameters(new DirStructure(@"D:\\", fileFactory), "data.xml"));
            //Then
            Assert.That(foundedFiles, Is.EqualTo(1));
        }

        [Test]
        public void FileSearchContains_Test()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            manager.SearchFiles( new SearchParameters(new DirStructure(@"D:\\", fileFactory), "my"), MatchOptions.Contains);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(8));
        }

        [Test]
        public async void FileSearchContains_AsyncTest()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            await manager.SearchFilesAsync(new SearchParameters(new DirStructure(@"D:\\", fileFactory), "my"), MatchOptions.Contains);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(8));
        }

        [Test]
        public async void FileSearchRegex_AsyncTest()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            await manager.SearchFilesAsync( new SearchParameters(new DirStructure(@"D:\\", fileFactory), @"[a-z]{2}[A-Z]{1}.*[2]{1}.*"),RegexOptions.None);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(1));
        }


        [Test]
        public void FileSearchRegexRecursive_Test()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            manager.SearchFiles(new SearchParameters(
                new DirStructure(@"D:\Projects\TestCup", fileFactory), @".*.xml", true), RegexOptions.IgnoreCase);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(8));
        }

        [Test]
        public async void FileSearchRegexRecursive_AsyncTest()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            await manager.SearchFilesAsync(new SearchParameters(
                new DirStructure(@"D:\Projects\TestCup", fileFactory), @".*.xml", true), RegexOptions.IgnoreCase);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(8));
        }
    }
}