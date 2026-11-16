using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Commander.ViewModel;
using IOLib;
using LogLib;
using NUnit.Framework;
using File = System.IO.File;

namespace CommanderTest
{
    [TestFixture]
    public class FileDiffVMTest
    {
        private string _root, _left, _right;
        private FakeDialogs _dialogs;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-filediff-" + Guid.NewGuid().ToString("N"))).FullName;
            _left = Path.Combine(_root, "left.txt");
            _right = Path.Combine(_root, "right.txt");
            _dialogs = new FakeDialogs();
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private async Task<FileDiffVM> Open(string left, string right)
        {
            File.WriteAllText(_left, left);
            File.WriteAllText(_right, right);
            var vm = new FileDiffVM(_left, _right, _dialogs, new ComplexLogger(new GUILogger()), null);
            await vm.Load();
            return vm;
        }

        private static string Kinds(FileDiffVM vm) => new string(vm.Rows.Select(r => "=~<>"[(int)r.Kind]).ToArray());

        [Test]
        public async Task Load_AlignsRows_AndPutsTheCursorOnTheFirstDifference()
        {
            var vm = await Open("a\nb\nc\nd\n", "a\nB\nc\nd\ne\n");
            Assert.That(Kinds(vm), Is.EqualTo("=~==>"));
            Assert.That(vm.DiffCount, Is.EqualTo(2));
            Assert.That(vm.CursorIndex, Is.EqualTo(1));
            Assert.That(vm.Rows[1].LeftSpans, Is.EqualTo(new[] { (0, 1) }));

            vm.NextDiff();
            Assert.That(vm.CursorIndex, Is.EqualTo(4));
            vm.NextDiff();
            Assert.That(vm.CursorIndex, Is.EqualTo(4), "stays on the last difference");
            vm.PrevDiff();
            Assert.That(vm.CursorIndex, Is.EqualTo(1));
        }

        [Test]
        public async Task CopyBlock_ToRight_ThenSave_MakesTheFilesEqual()
        {
            var vm = await Open("a\nb\nc\n", "a\nx\ny\nc\n");
            vm.CopyBlock(1);
            Assert.That(Kinds(vm), Is.EqualTo("==="));
            Assert.That(vm.RightDirty, Is.True);
            Assert.That(vm.LeftDirty, Is.False);
            Assert.That(File.ReadAllText(_right), Is.EqualTo("a\nx\ny\nc\n"), "nothing written before Save");

            await vm.Save();
            Assert.That(File.ReadAllText(_right), Is.EqualTo("a\nb\nc\n"));
            Assert.That(vm.HasChanges, Is.False);
        }

        [Test]
        public async Task CopyBlock_IntoAGap_InsertsTheLines()
        {
            var vm = await Open("a\nb\nc\n", "a\nc\n");
            vm.CopyBlock(1);
            await vm.Save();
            Assert.That(File.ReadAllText(_right), Is.EqualTo("a\nb\nc\n"));
        }

        [Test]
        public async Task EditLine_SplitJoin_AndUndo()
        {
            var vm = await Open("one\ntwo\n", "one\ntwo\n");
            vm.CursorIndex = 0;
            Assert.That(vm.BeginEdit(), Is.True);
            vm.Rows[0].EditText = "onetwo";
            vm.SplitLine(3);
            Assert.That(vm.IsEditing, Is.True, "keeps editing the new line");
            Assert.That(vm.Rows.Where(r => r.LeftNo >= 0).Select(r => r.Left), Is.EqualTo(new[] { "one", "two", "two" }));
            Assert.That(vm.Current.LeftNo, Is.EqualTo(1));

            vm.JoinLine();
            Assert.That(vm.Rows.Where(r => r.LeftNo >= 0).Select(r => r.Left), Is.EqualTo(new[] { "onetwo", "two" }));
            Assert.That(vm.EditCaret, Is.EqualTo(3));
            vm.CommitEdit();

            vm.Undo();
            vm.Undo();
            Assert.That(Kinds(vm), Is.EqualTo("=="));
            Assert.That(vm.HasChanges, Is.False, "undo back to the loaded text clears the dirty flag");
        }

        [Test]
        public async Task TypingOnAnEmptyRow_InsertsALine()
        {
            var vm = await Open("a\nc\n", "a\nb\nc\n");
            vm.CursorIndex = 1; // left side has no line here
            vm.BeginEdit();
            vm.Rows[1].EditText = "b";
            vm.CommitEdit();
            Assert.That(Kinds(vm), Is.EqualTo("==="));
        }

        [Test]
        public async Task DeleteLine_OnTheActiveSide()
        {
            var vm = await Open("a\nb\n", "a\nb\nc\n");
            vm.ActiveSide = 1;
            vm.CursorIndex = 2;
            vm.DeleteLine();
            Assert.That(Kinds(vm), Is.EqualTo("=="));
            await vm.Save();
            Assert.That(File.ReadAllText(_right), Is.EqualTo("a\nb\n"));
        }

        [Test]
        public async Task Save_FileChangedOnDisk_AsksFirst()
        {
            var vm = await Open("a\n", "b\n");
            vm.CopyBlock(1);
            File.WriteAllText(_right, "changed elsewhere\n");
            File.SetLastWriteTimeUtc(_right, DateTime.UtcNow.AddMinutes(1));
            _dialogs.ConfirmAnswer = false;
            await vm.Save();
            Assert.That(File.ReadAllText(_right), Is.EqualTo("changed elsewhere\n"));
            Assert.That(vm.RightDirty, Is.True);
        }

        [Test]
        public async Task SameTextDifferentEndings_IsNotReportedIdentical()
        {
            var vm = await Open("a\nb\n", "a\r\nb\r\n");
            Assert.That(vm.DiffCount, Is.EqualTo(0));
            Assert.That(vm.Status, Does.Contain("line endings"));
        }

        [Test]
        public async Task Binary_ShowsHexRows_AndCountsDifferingBytes()
        {
            File.WriteAllBytes(_left, Enumerable.Range(0, 40).Select(i => (byte)i).ToArray());
            var other = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
            other[17] = 0xFF;
            File.WriteAllBytes(_right, other.Take(36).ToArray());
            var vm = new FileDiffVM(_left, _right, _dialogs, new ComplexLogger(new GUILogger()), null);
            await vm.Load();

            Assert.That(vm.IsBinary, Is.True);
            Assert.That(Kinds(vm), Is.EqualTo("=~~"));
            Assert.That(vm.Rows[1].LeftLabel, Is.EqualTo("00000010"));
            Assert.That(vm.Rows[1].LeftSpans, Is.EqualTo(new[] { (3, 2), (50, 1) }));
            Assert.That(vm.Status, Does.Contain("5 bytes differ, first at 0x11"));
            Assert.That(vm.CopyToRightCommand.CanExecute(null), Is.False, "the hex view is read-only");
        }
    }
}
