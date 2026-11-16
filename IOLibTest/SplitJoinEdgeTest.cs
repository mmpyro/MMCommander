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

        // The Join window passes the first part; parts keep the extension so movie.bin and movie.txt can't collide.
        [Test]
        public void Split_ThenJoinFromFirstPart_RestoresFile()
        {
            var file = Path.Combine(_root, "movie.bin");
            File.WriteAllText(file, "0123456789");
            var parts = Directory.CreateDirectory(Path.Combine(_root, "parts")).FullName;
            _manager.Split(_factory.CreateFileMsg(file), 4, FileSizeUnit.B, parts);
            var output = Path.Combine(_root, "joined.bin");

            _manager.Join(parts, FileManager.PartBaseName(Path.Combine(parts, "movie.bin__0")), output);

            Assert.That(File.ReadAllText(output), Is.EqualTo("0123456789"));
        }

        // Parts used to drop the extension: movie.bin and movie.txt both wrote movie__0 and mixed their parts.
        [Test]
        public void Split_TwoFilesWithSameStem_BothJoinBack()
        {
            var bin = Path.Combine(_root, "movie.bin");
            var txt = Path.Combine(_root, "movie.txt");
            File.WriteAllText(bin, "BBBBBBBBBB");
            File.WriteAllText(txt, "tttttttttttt");
            var parts = Directory.CreateDirectory(Path.Combine(_root, "parts")).FullName;
            _manager.Split(_factory.CreateFileMsg(bin), 4, FileSizeUnit.B, parts);
            _manager.Split(_factory.CreateFileMsg(txt), 4, FileSizeUnit.B, parts);

            _manager.Join(parts, "movie.bin", Path.Combine(_root, "bin.out"));
            _manager.Join(parts, "movie.txt", Path.Combine(_root, "txt.out"));

            Assert.That(File.ReadAllText(Path.Combine(_root, "bin.out")), Is.EqualTo("BBBBBBBBBB"));
            Assert.That(File.ReadAllText(Path.Combine(_root, "txt.out")), Is.EqualTo("tttttttttttt"));
        }

        [Test]
        public void Join_MissingPart_Throws_AndWritesNothing()
        {
            foreach (var i in new[] { 0, 1, 3 })
                Write("movie__" + i, i + ";");
            var output = Path.Combine(_root, "joined");

            Assert.Throws<ArgumentException>(() => _manager.Join(_root, "movie", output));
            Assert.That(File.Exists(output), Is.False);
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
