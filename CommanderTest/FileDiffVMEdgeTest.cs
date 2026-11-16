using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commander.ViewModel;
using LogLib;
using NUnit.Framework;
using File = System.IO.File;

namespace CommanderTest
{
    // Edge cases of the compare files window model (test plan T19-T25, T30, T33).
    [TestFixture]
    public class FileDiffVMEdgeTest
    {
        private string _root, _left, _right;
        private FakeDialogs _dialogs;
        private int _refreshes;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-filediff-edge-" + Guid.NewGuid().ToString("N"))).FullName;
            _left = Path.Combine(_root, "left.txt");
            _right = Path.Combine(_root, "right.txt");
            _dialogs = new FakeDialogs();
            _refreshes = 0;
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private async Task<FileDiffVM> Open(byte[] left, byte[] right)
        {
            File.WriteAllBytes(_left, left);
            File.WriteAllBytes(_right, right);
            var vm = new FileDiffVM(_left, _right, _dialogs, new ComplexLogger(new GUILogger()), () => _refreshes++);
            await vm.Load();
            return vm;
        }

        private Task<FileDiffVM> Open(string left, string right) => Open(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

        private static string Kinds(FileDiffVM vm) => new string(vm.Rows.Select(r => "=~<>"[(int)r.Kind]).ToArray());

        private static void Edit(FileDiffVM vm, int side, int row, string text)
        {
            vm.ActiveSide = side;
            vm.CursorIndex = row;
            vm.BeginEdit();
            vm.Current.EditText = text;
            vm.CommitEdit();
        }

        [Test] // T19
        public async Task CopyBlock_ToLeft_ThenSave()
        {
            var vm = await Open("a\nb\nc\n", "a\nx\ny\nc\n");
            vm.CopyBlock(0);
            Assert.That(Kinds(vm), Is.EqualTo("===="));
            Assert.That(vm.LeftDirty, Is.True);
            Assert.That(vm.RightDirty, Is.False);
            await vm.Save();
            Assert.That(File.ReadAllText(_left), Is.EqualTo("a\nx\ny\nc\n"));
        }

        [Test] // T20
        public async Task CopyBlock_OnAnIdenticalRow_ChangesNothing()
        {
            var vm = await Open("a\nb\n", "a\nc\n");
            vm.CursorIndex = 0;
            vm.CopyBlock(1);
            Assert.That(Kinds(vm), Is.EqualTo("=~"));
            Assert.That(vm.HasChanges, Is.False);
            Assert.That(vm.UndoCommand.CanExecute(null), Is.False, "no undo step for a no-op");
        }

        [Test] // T21
        public async Task CopyBlock_AtTheEndOfTheFile_AppendsTheLines()
        {
            var vm = await Open("a\nb\nc\n", "a\n");
            vm.CursorIndex = 1;
            vm.CopyBlock(1);
            await vm.Save();
            Assert.That(File.ReadAllText(_right), Is.EqualTo("a\nb\nc\n"));
        }

        // T22 [assumption]: the target file keeps its own final newline, or its lack of one.
        [TestCase(1, "a\nb\n", TestName = "CopyBlock_UnterminatedLastLine_ToRight_KeepsTheRightFinalNewline")]
        [TestCase(0, "a\nx", TestName = "CopyBlock_UnterminatedLastLine_ToLeft_AddsNoFinalNewline")]
        public async Task CopyBlock_UnterminatedLastLine(int toSide, string expected)
        {
            var vm = await Open("a\nb", "a\nx\n");
            vm.CursorIndex = 1;
            vm.CopyBlock(toSide);
            await vm.Save();
            Assert.That(File.ReadAllText(toSide == 1 ? _right : _left), Is.EqualTo(expected));
        }

        [Test] // T23
        public async Task CommitEdit_PastedLineBreak_InsertsSeveralLines()
        {
            var vm = await Open("a\nc\n", "a\nc\n");
            Edit(vm, 0, 0, "a\nb");
            await vm.Save();
            Assert.That(File.ReadAllText(_left), Is.EqualTo("a\nb\nc\n"));
        }

        [Test] // T24
        public async Task Undo_KeepsTheLast100Steps()
        {
            var vm = await Open("0\n", "0\n");
            for (int i = 1; i <= 101; i++)
                Edit(vm, 0, 0, i.ToString());
            for (int i = 0; i < 100; i++)
                vm.Undo();
            Assert.That(vm.Rows[0].Left, Is.EqualTo("1"), "the oldest step (0 -> 1) was dropped");
            Assert.That(vm.UndoCommand.CanExecute(null), Is.False);
            vm.Undo();
            Assert.That(vm.Rows[0].Left, Is.EqualTo("1"), "an extra undo does nothing");
        }

        [Test] // T25
        public async Task Save_BothSidesChanged_WritesBoth_AndRefreshesThePanels()
        {
            var vm = await Open("a\n", "b\n");
            Edit(vm, 0, 0, "L");
            Edit(vm, 1, 0, "R");
            Assert.That(vm.LeftDirty && vm.RightDirty, Is.True);
            await vm.Save();
            Assert.That(File.ReadAllText(_left), Is.EqualTo("L\n"));
            Assert.That(File.ReadAllText(_right), Is.EqualTo("R\n"));
            Assert.That(vm.HasChanges, Is.False);
            Assert.That(_refreshes, Is.EqualTo(1));
        }

        [Test] // T26 through the model: the error is logged and the edit is kept.
        [Platform(Exclude = "Win"), System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
        public async Task Save_Fails_KeepsTheEditAndTheFile()
        {
            var vm = await Open("a\n", "b\n");
            Edit(vm, 0, 0, "L");
            File.SetUnixFileMode(_root, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            try
            {
                await vm.Save();
            }
            finally
            {
                File.SetUnixFileMode(_root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            Assert.That(vm.LeftDirty, Is.True);
            Assert.That(File.ReadAllText(_left), Is.EqualTo("a\n"));
            Assert.That(Directory.GetFiles(_root).Length, Is.EqualTo(2), "no temp file left");
        }

        [Test] // T30
        public async Task Reload_WithUnsavedEdits_AsksFirst()
        {
            var vm = await Open("a\n", "b\n");
            Edit(vm, 0, 0, "L");
            _dialogs.ConfirmAnswer = false;
            await vm.Reload();
            Assert.That(vm.Rows[0].Left, Is.EqualTo("L"), "No keeps the edit");
            Assert.That(await vm.ConfirmDiscard(), Is.False);

            _dialogs.ConfirmAnswer = true;
            await vm.Reload();
            Assert.That(vm.Rows[0].Left, Is.EqualTo("a"), "Yes reads the file again");
            Assert.That(vm.HasChanges, Is.False);
            Assert.That(vm.UndoCommand.CanExecute(null), Is.False);
        }

        [Test] // T33
        public async Task Binary_IdenticalFiles()
        {
            var bytes = new byte[] { 0, 1, 2, 3, 255 };
            var vm = await Open(bytes, bytes);
            Assert.That(vm.IsBinary, Is.True);
            Assert.That(vm.Status, Is.EqualTo("Files are identical"));
            Assert.That(vm.DiffCount, Is.EqualTo(0));
        }

        [Test] // T33
        public async Task Binary_AgainstAnEmptyFile()
        {
            var vm = await Open(new byte[0], new byte[] { 0, 1, 2 });
            Assert.That(vm.IsBinary, Is.True);
            Assert.That(Kinds(vm), Is.EqualTo(">"));
            Assert.That(vm.Status, Does.Contain("3 bytes differ, first at 0x0"));
        }

        [Test] // T33
        public async Task TextAgainstBinary_UsesTheHexView()
        {
            var vm = await Open(Encoding.UTF8.GetBytes("hello\n"), new byte[] { (byte)'h', 0 });
            Assert.That(vm.IsBinary, Is.True);
        }

        [Test] // T33
        public async Task Binary_DifferencePast4MB_IsCounted_AndTheTruncationIsSaid()
        {
            var a = new byte[5 * 1024 * 1024];
            var b = (byte[])a.Clone();
            b[0x480000] = 1;
            var vm = await Open(a, b);
            Assert.That(vm.DiffCount, Is.EqualTo(0), "the listed 4 MB are identical");
            Assert.That(vm.Status, Does.Contain("1 bytes differ, first at 0x480000"));
            Assert.That(vm.Status, Does.Contain("showing the first 4 MB"));
        }
    }
}
