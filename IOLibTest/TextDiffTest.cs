using System;
using System.IO;
using System.Linq;
using System.Text;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class TextDiffTest
    {
        private string _root;

        [SetUp]
        public void Before() => _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, "diff-" + Guid.NewGuid().ToString("N"))).FullName;

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private static string[] Split(string text) => text.Length == 0 ? new string[0] : text.Split(',');

        // left lines, right lines, expected row kinds: = same, ~ changed, < only left, > only right
        [TestCase("", "", "")]
        [TestCase("a,b,c", "a,b,c", "===")]
        [TestCase("a,c", "a,b,c", "=>=")]
        [TestCase("a,b,c", "a,c", "=<=")]
        [TestCase("a,b,c", "a,x,c", "=~=")]
        [TestCase("a,b,c,d", "a,x,d", "=~<=")]
        [TestCase("a,b", "a,x,y,b", "=>>=")]
        [TestCase("", "a,b", ">>")]
        [TestCase("a,b", "", "<<")]
        [TestCase("a,b", "x,y", "~~")]
        [TestCase("a,b,c,d,e", "b,c,x,e,f", "<==~=>")]
        public void Lines_Kinds(string left, string right, string expected)
        {
            var l = Split(left);
            var r = Split(right);
            var rows = TextDiff.Lines(l, r);
            Assert.That(new string(rows.Select(row => "=~<>"[(int)row.Kind]).ToArray()), Is.EqualTo(expected));
            // Both sides come back complete and in order.
            Assert.That(rows.Where(row => row.LeftNo >= 0).Select(row => row.Left), Is.EqualTo(l));
            Assert.That(rows.Where(row => row.RightNo >= 0).Select(row => row.Right), Is.EqualTo(r));
            Assert.That(rows.Where(row => row.LeftNo >= 0).Select(row => row.LeftNo), Is.EqualTo(Enumerable.Range(0, l.Length)));
            Assert.That(rows.Where(row => row.RightNo >= 0).Select(row => row.RightNo), Is.EqualTo(Enumerable.Range(0, r.Length)));
        }

        [Test]
        public void Lines_PastTheCap_StillReturnsEveryLine()
        {
            var l = Enumerable.Range(0, 5000).Select(i => "l" + i).ToArray();
            var r = Enumerable.Range(0, 5000).Select(i => "r" + i).ToArray();
            var rows = TextDiff.Lines(l, r);
            Assert.That(rows.Count, Is.EqualTo(5000));
            Assert.That(rows.All(row => row.Kind == DiffKind.Changed));
        }

        // Bug 1: 5,000 scattered changes (past the old Myers cap) stay 5,000 separate changed lines.
        [Test]
        public void Lines_ManyScatteredChanges_KeepsSeparateDifferences()
        {
            var l = Enumerable.Range(0, 20000).Select(i => "line " + i).ToArray();
            var r = l.Select((s, i) => i % 4 == 0 ? s + " changed" : s).ToArray();
            var rows = TextDiff.Lines(l, r);
            Assert.That(rows.Count(row => row.Kind == DiffKind.Changed), Is.EqualTo(5000));
            Assert.That(rows.Count(row => row.Kind == DiffKind.Same), Is.EqualTo(15000));
            Assert.That(rows.Count, Is.EqualTo(20000));
        }

        // Anchoring with repeated lines: whatever the alignment, nothing is lost or reordered, and "same" rows match.
        [Test]
        public void Lines_RandomInputs_KeepBothSidesComplete()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var random = new Random(seed);
                string[] Make() => Enumerable.Range(0, random.Next(0, 40))
                    .Select(i => random.Next(4) == 0 ? "u" + random.Next(30) : ((char)('a' + random.Next(3))).ToString()).ToArray();
                var l = Make();
                var r = Make();
                var rows = TextDiff.Lines(l, r);
                Assert.That(rows.Where(row => row.LeftNo >= 0).Select(row => row.Left), Is.EqualTo(l), "seed " + seed);
                Assert.That(rows.Where(row => row.RightNo >= 0).Select(row => row.Right), Is.EqualTo(r), "seed " + seed);
                Assert.That(rows.Where(row => row.LeftNo >= 0).Select(row => row.LeftNo), Is.EqualTo(Enumerable.Range(0, l.Length)), "seed " + seed);
                Assert.That(rows.Where(row => row.RightNo >= 0).Select(row => row.RightNo), Is.EqualTo(Enumerable.Range(0, r.Length)), "seed " + seed);
                Assert.That(rows.Where(row => row.Kind == DiffKind.Same).All(row => row.Left == row.Right), Is.True, "seed " + seed);
            }
        }

        [TestCase("int b = 2;", "int b = 3;", "8+1", "8+1")]
        [TestCase("abc", "abc", "", "")]
        [TestCase("abc", "abXc", "", "2+1")]
        [TestCase("one two", "two", "0+4", "")]
        public void Spans_MarkTheDifferingCharacters(string a, string b, string expectedA, string expectedB)
        {
            TextDiff.Spans(a, b, out var inA, out var inB);
            Assert.That(string.Join(",", inA.Select(s => s.Start + "+" + s.Length)), Is.EqualTo(expectedA));
            Assert.That(string.Join(",", inB.Select(s => s.Start + "+" + s.Length)), Is.EqualTo(expectedB));
        }

        private string Put(byte[] bytes)
        {
            var path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static readonly byte[][] TextFiles =
        {
            new byte[0],
            Encoding.UTF8.GetBytes("one\ntwo\n"),
            Encoding.UTF8.GetBytes("one\r\ntwo\nthree\rfour"),
            Encoding.UTF8.GetBytes("\n\nzażółć\n"),
            new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("bom\r\n")).ToArray(),
            Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("utf16\r\nline")).ToArray(),
            new byte[] { (byte)'a', 0xE9, (byte)'\n', 0xFF, (byte)'b' }, // not valid UTF-8
        };

        [TestCaseSource(nameof(TextFiles))]
        public void Save_Untouched_WritesTheSameBytes(byte[] bytes)
        {
            var path = Put(bytes);
            var doc = TextDoc.Load(path);
            Assert.That(doc, Is.Not.Null);
            doc.Save();
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            Assert.That(doc.ChangedOnDisk, Is.False);
            Assert.That(Directory.GetFiles(_root).Length, Is.EqualTo(1), "no temp file left behind");
        }

        [Test]
        public void Load_SplitsLines_AndIgnoresEndingsWhenComparing()
        {
            var unix = TextDoc.Load(Put(Encoding.UTF8.GetBytes("a\nb\n")));
            var dos = TextDoc.Load(Put(Encoding.UTF8.GetBytes("a\r\nb\r\n")));
            Assert.That(unix.Lines, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(dos.NewLine, Is.EqualTo("\r\n"));
            Assert.That(TextDiff.Lines(unix.Lines, dos.Lines).All(r => r.Kind == DiffKind.Same));
        }

        [Test]
        public void Edit_KeepsEncodingAndEndings()
        {
            var path = Put(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("a\r\nb")).ToArray());
            var doc = TextDoc.Load(path);
            doc.Lines[0] = "ż";
            doc.Insert(2, "c");
            doc.Save();
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("ż\r\nb\r\nc\r\n")).ToArray()));
        }

        [Test]
        public void Load_Binary_ReturnsNull()
        {
            Assert.That(TextDoc.Load(Put(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0, 1, 2 })), Is.Null);
        }

        [Test]
        public void ChangedOnDisk_SeesALaterWrite()
        {
            var path = Put(Encoding.UTF8.GetBytes("a\n"));
            var doc = TextDoc.Load(path);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(5));
            Assert.That(doc.ChangedOnDisk, Is.True);
        }
    }
}
