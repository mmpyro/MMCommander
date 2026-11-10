using System;
using System.IO;
using System.Linq;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class SplitJoinEdgeTest
    {
        private string _root;
        private readonly FileFactory _factory = new FileFactory();
        private FileManager _manager;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, Guid.NewGuid().ToString("N"))).FullName;
            _manager = new FileManager(_factory);
        }

        [TearDown]
        public void After() => _factory.CreateFileMsg(_root).Delete();

        private void Write(string name, string content) => File.WriteAllText(Path.Combine(_root, name), content);

        // BUG-15: the join pattern was unescaped and unanchored.
        [TestCase("movie")]
        [TestCase("a+b")]
        [TestCase("x[1]")]
        public void Join_TakesOnlyOwnParts_InNumericOrder(string name)
        {
            for (int i = 0; i < 11; i++)
                Write(name + "__" + i, i + ";");
            Write("old_" + name + "__1", "WRONG");
            Write(name + "__x", "WRONG");
            var output = Path.Combine(_root, "joined");

            _manager.Join(_root, name, output);

            Assert.That(File.ReadAllText(output), Is.EqualTo(string.Concat(Enumerable.Range(0, 11).Select(i => i + ";"))));
        }

        // BUG-07: 3 GB overflowed int and the size check passed with a negative number.
        [Test]
        public void Split_PartLargerThanFile_InGigabytes_Throws()
        {
            var file = Path.Combine(_root, "small");
            File.WriteAllText(file, "abc");
            Assert.Throws<ArgumentException>(() => _manager.Split(_factory.CreateFileMsg(file), 3, FileSizeUnit.GB, _root));
        }

        // BUG-07: offsets past int.MaxValue. Writes about 2.2 GB, so it only runs on request.
        [Test, Explicit("writes 2.2 GB to the temp folder")]
        public void Split_Over2GB_WritesAllParts()
        {
            var file = Path.Combine(_root, "big");
            long size = (long)int.MaxValue + 100_000_000;
            using (var stream = File.Create(file))
            {
                stream.SetLength(size);
                stream.Seek(-1, SeekOrigin.End);
                stream.WriteByte(42);
            }
            _manager.Split(_factory.CreateFileMsg(file), 1, FileSizeUnit.GB, _root);
            var parts = Enumerable.Range(0, 3).Select(i => new FileInfo(Path.Combine(_root, "big__" + i))).ToList();
            Assert.That(parts.Sum(p => p.Length), Is.EqualTo(size));
            using var last = File.OpenRead(parts[2].FullName);
            last.Seek(-1, SeekOrigin.End);
            Assert.That(last.ReadByte(), Is.EqualTo(42));
        }
    }
}
