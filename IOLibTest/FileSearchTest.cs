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
            manager.SearchFiles( new SearchParameters(@"D:\\", "data.xml"));
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
            manager.SearchFiles( new SearchParameters(@"D:\\", "my"), MatchOptions.Contains);
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
            await manager.SearchFilesAsync(new SearchParameters(@"D:\\","my"), MatchOptions.Contains);
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
            await manager.SearchFilesAsync( new SearchParameters(@"D:\\", @"[a-z]{2}[A-Z]{1}.*[2]{1}.*"),RegexOptions.None);
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
                @"D:\Projects\TestCup", @".*.xml", true), RegexOptions.IgnoreCase);
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
                @"D:\Projects\TestCup", @".*.xml", true), RegexOptions.IgnoreCase);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(8));
        }

    }
}