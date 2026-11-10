using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class SearchEdgeTest
    {
        private string _root;
        private readonly FileFactory _factory = new FileFactory();

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, Guid.NewGuid().ToString("N"))).FullName;
            Directory.CreateDirectory(Path.Combine(_root, "src", "deep"));
            foreach (var file in new[] { "data.js", "metadata.js", "Data.txt", "src/main.cs", "src/deep/data.js" })
                File.WriteAllText(Path.Combine(_root, file), "");
        }

        [TearDown]
        public void After()
        {
            // Locked folders sit directly under _root; unlock them before deleting.
            foreach (var dir in Directory.GetDirectories(_root))
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            _factory.CreateFileMsg(_root).Delete();
        }

        private List<string> Search(string phrase, MatchOptions match, bool recursive, RegexOptions options = RegexOptions.None)
        {
            var found = new List<string>();
            var manager = new DirectoryManager(_factory);
            manager.OnFindFile += f => found.Add(Path.GetRelativePath(_root, f.FullName).Replace('\\', '/'));
            manager.SearchFiles(new SearchParameters(_root, phrase, recursive) { MatchOptions = match, RegexOptions = options });
            return found.OrderBy(f => f).ToList();
        }

        // BUG-12: the pattern ran on the full path, so a folder name matched every file inside it.
        [Test]
        public void Search_MatchesFileNameOnly()
        {
            Assert.That(Search("src", MatchOptions.Contains, true), Is.Empty);
        }

        [Test]
        public void WholeWord_DoesNotMatchInsideAnotherWord()
        {
            Assert.That(Search("data", MatchOptions.WholeWord, false), Is.EqualTo(new[] { "data.js" }));
        }

        // BUG-12: recursion used to drop MatchOptions and RegexOptions.
        [Test]
        public void Recursive_KeepsOptionsInSubfolders()
        {
            Assert.That(Search("DATA", MatchOptions.WholeWord, true, RegexOptions.IgnoreCase),
                Is.EqualTo(new[] { "data.js", "Data.txt", "src/deep/data.js" }));
        }

        // BUG-13: one unreadable folder used to abort the whole search.
        [Test]
        public void Recursive_SkipsUnreadableFolder_AndContinues()
        {
            Assume.That(OperatingSystem.IsWindows(), Is.False, "uses Unix permissions");
            var locked = Directory.CreateDirectory(Path.Combine(_root, "aaa-locked")).FullName;
            File.SetUnixFileMode(locked, UnixFileMode.None);
            Assert.That(Search("main", MatchOptions.Contains, true), Is.EqualTo(new[] { "src/main.cs" }));
        }

        // BUG-13: listing an unreadable folder used to return an empty list.
        [Test]
        public void Listing_UnreadableFolder_Throws()
        {
            Assume.That(OperatingSystem.IsWindows(), Is.False, "uses Unix permissions");
            var locked = Directory.CreateDirectory(Path.Combine(_root, "locked")).FullName;
            File.SetUnixFileMode(locked, UnixFileMode.None);
            Assert.Throws<UnauthorizedAccessException>(() => new DirectoryManager(_factory).GetFilesFromPath(locked));
        }
    }
}
