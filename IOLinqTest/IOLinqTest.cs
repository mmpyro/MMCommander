using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IOLib;
using IOLinq;
using Moq;
using NUnit.Framework;

namespace IOLinqTest
{
    [TestFixture]
    public class IOLinqTest
    {
        private string[] _names;
        private string[] _exts;
        private string[][] _parameters;
        private FileFakeCreator _fileFakeCreator;
        private readonly Mock<IFileFactory> _mock = new Mock<IFileFactory>();
        private SyntaxParser _parser;

        [TestFixtureSetUp]
        public void Initialize()
        {
            _names = new string[]{"file","File","FILE","aa","#$^&&*^*","wasd562f"};
            _exts = new string[]{"txt","exe","pdf","xml","html","js"};
            _parameters = (from name in _names join ext in _exts on 1 equals 1 select new string[] {name, ext}).ToArray();
            _fileFakeCreator = new FileFakeCreator();
            _mock.Setup(t => t.CreateEmptyFileList()).Returns(new List<IAbstractFileStructure>());
            _parser = new SyntaxParser(_mock.Object);
        }

        [TestCase("exe")]
        [TestCase("pdf")]
        [TestCase("js")]
        public void FilterByExt_Test(string value)
        {
            //Given
            string ext = string.Format("ext == {0}", value);
            var list = _fileFakeCreator.CreateFiles(_parameters).ToArray();
            //When
            var result = _parser.Perform(ext, list.ToArray());
            //Then
            Assert.That(result.Count, Is.EqualTo(6));
        }

        [TestCase("exe,pdf,txt", 18)]
        [TestCase("pdf,js, html", 18)]
        [TestCase("xml, html", 12)]
        public void FilterByExtSet_Test(string value, int count)
        {
            //Given
            string ext = string.Format("ext -> ({0})", value);
            var list = _fileFakeCreator.CreateFiles(_parameters);
            //When
            var result = _parser.Perform(ext, list.ToArray());
            //Then
            Assert.That(result.Count, Is.EqualTo(count));
        }

        [TestCase("File",18)]
        [TestCase("#$^&&*^*", 0)]
        [TestCase("wasd562f", 6)]
        public void FilterByName_Test(string value, int count)
        {
            //Given
            string ext = string.Format("name == {0}", value);
            var list = _fileFakeCreator.CreateFiles(_parameters);
            //When
            var result = _parser.Perform(ext, list.ToArray());
            //Then
            Assert.That(result.Count, Is.EqualTo(count));
        }

        [TestCase("il",18)]
        public void FilterByContentName_Test(string value, int count)
        {
            //Given
            string ext = string.Format("name like {0}", value);
            var list = _fileFakeCreator.CreateFiles(_parameters);
            //When
            var result = _parser.Perform(ext, list.ToArray());
            //Then
            Assert.That(result.Count, Is.EqualTo(count));
        }

        [TestCase("name == aa & ext == pdf", 1)]
        [TestCase("name == aa | ext == pdf", 11)]
        [TestCase("ext -> (xml, pdf) & name == file", 6)]
        [TestCase("ext -> (xml, pdf) & name like il", 6)]
        public void ComplexFilter_Test(string value, int count)
        {
            //Given
            string ext = string.Format("{0}", value);
            var list = _fileFakeCreator.CreateFiles(_parameters);
            //When
            var result = _parser.Perform(ext, list.ToArray());
            //Then
            Assert.That(result.Count, Is.EqualTo(count));
        }
    }
}
