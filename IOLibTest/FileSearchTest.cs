using System.Text.RegularExpressions;
using IOLib;
using NUnit.Framework;
using System.IO;
using IOLibTest.helpers;
using System;

namespace IOLibTest
{
    [TestFixture]
    public class FileSearchTest
    {
        private readonly string DirectoryPath = Path.Combine(TestPaths.Root, "test_dir");

        [SetUp]
        public void Before()
        {
            FileHelper.CreateDirIfNotExist(DirectoryPath);
            FileHelper.CreateIfNotExist(Path.Combine(DirectoryPath, "data.js"));
            for(int i =0;i<5;i++)
                FileHelper.CreateIfNotExist(Path.Combine(DirectoryPath, string.Format("myZ{0}.js",i)));
        }

        // test_dir is shared with JoinFileTest, which counts every file in it.
        [TearDown]
        public void After() => FileHelper.DeleteAllFilesInDirectory(DirectoryPath);

        [Test]
        public void FileSearchByName_Test()
        {
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            //When
            manager.SearchFiles( new SearchParameters(DirectoryPath, "data.js"));
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
            var searchParameters = new SearchParameters(DirectoryPath, "my");
            searchParameters.MatchOptions = MatchOptions.Contains;
            //When
            manager.SearchFiles(searchParameters);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(5));
        }

        [Test]
        public void FileSearchRegex_Test()
        {
            Console.WriteLine(DirectoryPath);
            //Given
            int foundedFiles = 0;
            var fileFactory = new FileFactory();
            var manager = new DirectoryManager(fileFactory);
            manager.OnFindFile += (t) => { foundedFiles++; };
            var searchParameters = new SearchParameters(DirectoryPath, @"[a-z]{2}[A-Z]{1}[0-9]?.*");
            searchParameters.RegexOptions = RegexOptions.None;
            //When
            manager.SearchFiles(searchParameters);
            //Then
            Assert.That(foundedFiles, Is.EqualTo(5));
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
            manager.SearchFiles(new SearchParameters(DirectoryPath, @".*.js", true));
            //Then
            Assert.That(foundedFiles, Is.EqualTo(6));
        }

    }
}