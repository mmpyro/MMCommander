using System;
using System.IO;
using System.Linq;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class DirComparerTest
    {
        private string _root, _left, _right;
        private static readonly DateTime T0 = new DateTime(2024, 11, 27, 15, 37, 0), T1 = T0.AddDays(10);

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, "cmp-" + Guid.NewGuid().ToString("N"))).FullName;
            _left = Directory.CreateDirectory(Path.Combine(_root, "L")).FullName;
            _right = Directory.CreateDirectory(Path.Combine(_root, "R")).FullName;
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private static void Put(string dir, string name, string text, DateTime time)
        {
            var path = Path.Combine(dir, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
            File.SetLastWriteTime(path, time);
        }

        // left text/time, right text/time ("" = missing), mode, expected
        [TestCase("abc", 0, "", 0, CompareMode.NameSizeDate, CompareKind.OnlyLeft)]
        [TestCase("", 0, "abc", 0, CompareMode.NameSizeDate, CompareKind.OnlyRight)]
        [TestCase("abc", 0, "abc", 0, CompareMode.NameSizeDate, CompareKind.Identical)]
        [TestCase("abc", 1, "abc", 0, CompareMode.NameSizeDate, CompareKind.NewerLeft)]
        [TestCase("abc", 0, "abcd", 1, CompareMode.NameSizeDate, CompareKind.NewerRight)]
        [TestCase("abc", 0, "abcd", 0, CompareMode.NameSizeDate, CompareKind.Different)]
        [TestCase("abc", 0, "abd", 0, CompareMode.NameSizeDate, CompareKind.Identical)]
        [TestCase("abc", 0, "abd", 0, CompareMode.Content, CompareKind.Different)]
        [TestCase("abc", 1, "abc", 0, CompareMode.Content, CompareKind.Identical)]
        [TestCase("abc", 0, "xyzw", 1, CompareMode.Name, CompareKind.Identical)]
        public void File_Kinds(string leftText, int leftTime, string rightText, int rightTime, CompareMode mode, CompareKind expected)
        {
            if (leftText != "") Put(_left, "f.txt", leftText, leftTime == 0 ? T0 : T1);
            if (rightText != "") Put(_right, "f.txt", rightText, rightTime == 0 ? T0 : T1);
            var row = DirComparer.Compare(_left, _right, mode, false).Single();
            Assert.That(row.Kind, Is.EqualTo(expected));
        }

        [Test]
        public void Recursive_DescendsIntoSharedFolders_FoldersFirst()
        {
            Put(_left, "b.txt", "x", T0);
            Put(_left, "sub/inner.txt", "x", T0);
            Put(_right, "sub/inner.txt", "x", T1);
            Directory.CreateDirectory(Path.Combine(_left, "onlyleft"));
            Put(_left, "onlyleft/hidden.txt", "x", T0);

            var flat = DirComparer.Compare(_left, _right, CompareMode.NameSizeDate, false);
            Assert.That(flat.Select(r => r.RelativePath), Is.EqualTo(new[] { "onlyleft", "sub", "b.txt" }));

            var deep = DirComparer.Compare(_left, _right, CompareMode.NameSizeDate, true);
            Assert.That(deep.Select(r => r.RelativePath), Is.EqualTo(new[] { "onlyleft", "sub", "sub/inner.txt", "b.txt" }),
                "only folders on both sides are opened");
            Assert.That(deep.Single(r => r.RelativePath == "sub/inner.txt").Kind, Is.EqualTo(CompareKind.NewerRight));
        }
    }
}
