using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Comander;
using Comander.CommanderIO;
using Comander.Dtos;
using Comander.View;
using MainVM = Comander.ViewModel.MainVM;
using LogLib;
using NUnit.Framework;

namespace ComanderTest
{
    // User flows through the real MainWindow with headless keyboard and mouse input.
    // Never closes MainWindow: closing it saves the panel paths into the user's real Config.xml.
    [TestFixture]
    public class UiFlowTest
    {
        private string _root, _dest;
        private Window _window;
        private MainVM _vm;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-ui-" + Guid.NewGuid().ToString("N"))).FullName;
            Directory.CreateDirectory(Path.Combine(_root, "child"));
            _dest = Directory.CreateDirectory(Path.Combine(_root, "dest")).FullName;
            File.WriteAllText(Path.Combine(_root, "file.txt"), "hello");
            File.WriteAllText(Path.Combine(_root, "notes.md"), "");
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private static void Pump(int rounds = 20)
        {
            for (int i = 0; i < rounds; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(20); }
        }

        private static void PumpUntil(Func<bool> done, int timeoutMs = 5000)
        {
            var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!done() && DateTime.UtcNow < end) Pump(1);
            Pump(5);
        }

        private void Shot(string name)
        {
            var dir = Environment.GetEnvironmentVariable("MMC_SCREENSHOTS");
            if (!string.IsNullOrEmpty(dir))
                _window.CaptureRenderedFrame().Save(Path.Combine(dir, "flow-" + name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        private void Open(string left, string right)
        {
            _vm = App.CreateMainVm(new ComplexLogger(new GUILogger()));
            _window = new MainWindow { DataContext = _vm, Width = 1200, Height = 700 };
            _window.Show();
            _vm.Io1.ActualPath = left;
            _vm.Io2.ActualPath = right;
            Pump();
        }

        private DataGrid Grid(int panel) => _window.GetVisualDescendants().OfType<DataGrid>().ElementAt(panel - 1);

        private string[] RowNames(int panel) => Grid(panel).GetVisualDescendants().OfType<DataGridRow>()
            .Where(r => r.IsVisible).Select(r => (r.DataContext as IMetadataFileStructure)?.Name).Where(n => n != null).ToArray();

        private void Click(Control control)
        {
            var p = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), _window).Value;
            _window.MouseDown(p, MouseButton.Left);
            _window.MouseUp(p, MouseButton.Left);
            Pump();
        }

        private void ClickRow(int panel, string name) =>
            Click(Grid(panel).GetVisualDescendants().OfType<DataGridRow>().First(r => (r.DataContext as IMetadataFileStructure)?.Name == name));

        private void Key(Key key, RawInputModifiers mods = RawInputModifiers.None)
        {
            _window.KeyPress(key, mods, PhysicalKey.None, null);
            _window.KeyRelease(key, mods, PhysicalKey.None, null);
            Pump();
        }

        private void Type(string text)
        {
            foreach (var c in text) _window.KeyTextInput(c.ToString());
            Pump();
        }

        // Panel 1's CommandWindow: [0] path box, [1] selected-file box (read only), [2] filter box.
        // The drive ComboBox has its own inner TextBox, so skip TextBoxes inside a ComboBox.
        private TextBox PanelTextBox(int index) => _window.GetVisualDescendants().OfType<CommandWindow>().First()
            .GetVisualDescendants().OfType<TextBox>().Where(t => t.FindAncestorOfType<ComboBox>() == null).ElementAt(index);

        [AvaloniaTest]
        public void TypingPath_CharByChar_ListsThatFolder()
        {
            Open(_dest, _dest);
            var box = PanelTextBox(0);
            Click(box);
            box.SelectAll();
            Type(_root);
            PumpUntil(() => RowNames(1).Contains("child"));
            Shot("typed-path");

            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(_root));
            Assert.That(RowNames(1), Is.SupersetOf(new[] { "child", "dest", "file.txt", "notes.md" }));
        }

        [AvaloniaTest]
        public void EnterThenEscape_ReturnsToParent()
        {
            Open(_root, _dest);
            ClickRow(1, "child");
            Key(Avalonia.Input.Key.Enter);
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(Path.Combine(_root, "child")));
            Key(Avalonia.Input.Key.Escape);
            Shot("escape-parent");
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(_root));
        }

        [AvaloniaTest]
        public void Tab_MovesFocusToOtherPanel()
        {
            Open(_root, _root);
            ClickRow(1, "file.txt");
            Assert.That(Grid(1).IsKeyboardFocusWithin, Is.True, "panel 1 focused after click");
            Key(Avalonia.Input.Key.Tab);
            Shot("tab");
            Assert.That(Grid(2).IsKeyboardFocusWithin, Is.True, "panel 2 focused after Tab");
        }

        [AvaloniaTest]
        public void S_SelectsFile_AndShowsCount()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.S);
            Shot("select");
            Assert.That(_vm.Io1.Files.Single(f => f.Name == "file.txt").IsSelected(), Is.True);
            Assert.That(_vm.Io1.CurrentOperation, Is.EqualTo("Selected elements: 1"));
            // Marking several files with S in a row needs the cursor to stay where it was.
            Assert.That(_vm.Io1.SelectedFile?.Name, Is.EqualTo("file.txt"), "cursor stays on the marked file");
        }

        [AvaloniaTest]
        public void SelectAll_ThenReverse_SelectsNothing()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.A);
            var all = _vm.Io1.Files.Count(f => f.IsSelected());
            Key(Avalonia.Input.Key.R);
            Shot("select-all-reverse");
            Assert.That(all, Is.EqualTo(4), "child, dest, file.txt, notes.md");
            Assert.That(_vm.Io1.Files.Count(f => f.IsSelected()), Is.Zero);
        }

        [AvaloniaTest]
        public void SelectThenC_CopiesFileIntoOtherPanel()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.S);
            Key(Avalonia.Input.Key.C);
            PumpUntil(() => RowNames(2).Contains("file.txt"));
            Shot("copy");
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("hello"));
            Assert.That(File.Exists(Path.Combine(_root, "file.txt")), Is.True, "source kept");
            Assert.That(RowNames(2), Does.Contain("file.txt"));
        }

        [AvaloniaTest]
        public void SelectThenM_MovesFileIntoOtherPanel()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.S);
            Key(Avalonia.Input.Key.M);
            PumpUntil(() => RowNames(2).Contains("file.txt") && !RowNames(1).Contains("file.txt"));
            Shot("move");
            Assert.That(File.Exists(Path.Combine(_dest, "file.txt")), Is.True);
            Assert.That(File.Exists(Path.Combine(_root, "file.txt")), Is.False);
            Assert.That(RowNames(1), Does.Not.Contain("file.txt"));
            Assert.That(RowNames(2), Does.Contain("file.txt"));
        }

        [AvaloniaTest]
        public void FilterBox_ShowsOnlyMatchingFiles()
        {
            Open(_root, _dest);
            var box = PanelTextBox(2);
            Click(box);
            Type("ext == txt");

            PumpUntil(() => !RowNames(1).Contains("notes.md"), 4000);
            Shot("filter");
            Assert.That(RowNames(1), Does.Contain("file.txt"));
            Assert.That(RowNames(1), Does.Not.Contain("notes.md"));
        }

        [AvaloniaTest]
        public void InputDialog_TypedTextIsReturnedOnEnter_NullOnEscape()
        {
            Open(_root, _dest);
            var ok = new InputWindow("Please enter the directory name", "new folder");
            var okResult = ok.ShowDialog<string>(_window);
            Pump();
            ok.KeyTextInput("docs");
            ok.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Pump();
            Assert.That(okResult.IsCompleted, Is.True, "Enter closes the dialog");
            Assert.That(okResult.Result, Is.EqualTo("docs"), "initial text is selected, so typing replaces it");

            var cancel = new InputWindow("Please enter the directory name", "x");
            var cancelResult = cancel.ShowDialog<string>(_window);
            Pump();
            cancel.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump();
            Assert.That(cancelResult.IsCompleted, Is.True, "Escape closes the dialog");
            Assert.That(cancelResult.Result, Is.Null);
        }

        [AvaloniaTest]
        public void ConfirmAndOverrideDialogs_KeyboardAnswers()
        {
            Open(_root, _dest);
            var yes = new ConfirmWindow("Do you want delete 1 files ?");
            Task<bool> yesResult = yes.ShowDialog<bool>(_window);
            Pump();
            yes.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Pump();
            Assert.That(yesResult.IsCompleted && yesResult.Result, Is.True, "Enter confirms");

            var no = new ConfirmWindow("Do you want delete 1 files ?");
            Task<bool> noResult = no.ShowDialog<bool>(_window);
            Pump();
            no.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump();
            Assert.That(noResult.IsCompleted, Is.True, "Escape closes confirm");
            Assert.That(noResult.Result, Is.False);

            var over = new OverrideWindow("File a.txt exist in /tmp.\nDo you want to override it ?");
            var overResult = over.ShowDialog<CopyOption>(_window);
            Pump();
            over.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump();
            Assert.That(overResult.IsCompleted, Is.True, "Escape closes override");
            Assert.That(overResult.Result, Is.EqualTo(CopyOption.Cancel));
        }
    }
}
