using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Commander;
using Commander.CommanderIO;
using Commander.Core;
using Commander.Dtos;
using Commander.View;
using MainVM = Commander.ViewModel.MainVM;
using LogLib;
using NUnit.Framework;

namespace CommanderTest
{
    // User flows through the real MainWindow with headless keyboard and mouse input.
    // Never closes MainWindow: closing it saves the panel paths into the user's real Config.xml.
    [TestFixture]
    public class UiFlowTest
    {
        private string _root, _dest;
        private Window _window;
        private MainVM _vm;
        private GUILogger _gui;

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
        public void After()
        {
            // A dialog left open would keep its transfer job (and the shared busy lock) waiting for later tests.
            foreach (var owned in _window?.OwnedWindows.ToList() ?? new System.Collections.Generic.List<Window>())
                owned.Close();
            Pump(5);
            Directory.Delete(_root, true);
        }

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

        private static void ShotOf(Window window, string name)
        {
            var dir = Environment.GetEnvironmentVariable("MMC_SCREENSHOTS");
            if (!string.IsNullOrEmpty(dir))
                window.CaptureRenderedFrame()?.Save(Path.Combine(dir, "flow-" + name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        private void Shot(string name)
        {
            var dir = Environment.GetEnvironmentVariable("MMC_SCREENSHOTS");
            if (!string.IsNullOrEmpty(dir))
                _window.CaptureRenderedFrame().Save(Path.Combine(dir, "flow-" + name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        private void Open(string left, string right)
        {
            _gui = new GUILogger();
            _vm = App.CreateMainVm(new ComplexLogger(_gui));
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

        // Click a control inside another window (a dialog): input goes to that window, in its coordinates.
        private static void ClickIn(Window window, Control control)
        {
            var p = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window).Value;
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
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

        // The modal dialog the app opened over MainWindow (F5 options, delete, ...).
        private Window Dialog<T>() where T : Window
        {
            PumpUntil(() => _window.OwnedWindows.OfType<T>().Any());
            return _window.OwnedWindows.OfType<T>().Last();
        }

        private void Answer<T>(Key key) where T : Window
        {
            var dialog = Dialog<T>();
            // No KeyRelease: Enter/Esc close the dialog on the press.
            dialog.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            Pump();
        }

        private void Type(string text)
        {
            foreach (var c in text) _window.KeyTextInput(c.ToString());
            Pump();
        }

        private DataGridRow Row(int panel, string name) => Grid(panel).GetVisualDescendants().OfType<DataGridRow>()
            .First(r => (r.DataContext as IMetadataFileStructure)?.Name == name);

        private Commander.Controls.FilePane Pane(int panel) =>
            _window.GetVisualDescendants().OfType<Commander.Controls.FilePane>().ElementAt(panel - 1);

        private T Named<T>(int panel, string name) where T : Control =>
            Pane(panel).GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        [AvaloniaTest]
        public void TypingPath_CharByChar_ListsThatFolder()
        {
            Open(_dest, _dest);
            // The path shows as a breadcrumb; clicking its empty space turns it into a text box.
            var bar = Named<Border>(1, "PathBar");
            var end = bar.TranslatePoint(new Point(bar.Bounds.Width - 4, bar.Bounds.Height / 2), _window).Value;
            _window.MouseDown(end, MouseButton.Left);
            _window.MouseUp(end, MouseButton.Left);
            Pump();
            var box = Named<TextBox>(1, "PathBox");
            Assert.That(box.IsVisible && box.IsFocused, Is.True, "path box is in edit mode");
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
            Assert.That(_vm.ActivePanel, Is.SameAs(_vm.Io1));
            Key(Avalonia.Input.Key.Tab);
            Shot("tab");
            Assert.That(Grid(2).IsKeyboardFocusWithin, Is.True, "panel 2 focused after Tab");
            Assert.That(_vm.ActivePanel, Is.SameAs(_vm.Io2), "toolbar and F-keys follow the focused panel");
            Assert.That(_vm.Io1.IsActive, Is.False);
            Assert.That(_vm.Io2.IsActive, Is.True);
        }

        [AvaloniaTest]
        public void SpaceThenF5_CopiesFileIntoOtherPanel()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.Space);
            Assert.That(_vm.Io1.Files.Single(f => f.Name == "file.txt").IsSelected(), Is.True, "Space marks");
            Key(Avalonia.Input.Key.F5);
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
            PumpUntil(() => RowNames(2).Contains("file.txt"));
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("hello"));
        }

        [AvaloniaTest]
        public void FKeyBarCopy_ActsOnActivePanel()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.Space);
            var f5 = _window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("fkey")
                && b.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "F5"));
            Click(f5);
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
            PumpUntil(() => RowNames(2).Contains("file.txt"));
            Shot("fkeys");
            Assert.That(File.Exists(Path.Combine(_dest, "file.txt")), Is.True);
        }

        [AvaloniaTest]
        public void S_SelectsFile_AndShowsCount()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Assert.That(Row(1, "file.txt").Classes, Does.Not.Contain("marked"));
            Key(Avalonia.Input.Key.S);
            Shot("select");
            Assert.That(Row(1, "file.txt").Classes, Does.Contain("marked"), "row redraws as marked");
            Assert.That(Named<TextBlock>(1, "SelectionText").Text, Is.EqualTo("1 selected · 0 KB"));
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
            Shot("copy-dialog");
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
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
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
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
            var box = Named<TextBox>(1, "FilterBox");
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
            var ok = new InputWindow("New folder", "new folder");
            var okResult = ok.ShowDialog<string>(_window);
            Pump();
            ok.KeyTextInput("docs");
            ok.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Pump();
            Assert.That(okResult.IsCompleted, Is.True, "Enter closes the dialog");
            Assert.That(okResult.Result, Is.EqualTo("docs"), "initial text is selected, so typing replaces it");

            var cancel = new InputWindow("New folder", "x");
            var cancelResult = cancel.ShowDialog<string>(_window);
            Pump();
            cancel.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump();
            Assert.That(cancelResult.IsCompleted, Is.True, "Escape closes the dialog");
            Assert.That(cancelResult.Result, Is.Null);
        }

        // One Enter on the options page shows the results page and runs the search (no second "Search" press).
        [AvaloniaTest]
        public void Search_EnterOnOptionsPage_ShowsResults()
        {
            Open(_root, _dest);
            var search = new Search.SearchWindow(_vm.Io1, new ComplexLogger(_gui));
            search.Show(_window);
            Pump();
            search.KeyTextInput("file");
            search.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

            ListBox Results() => search.GetVisualDescendants().OfType<ListBox>().FirstOrDefault();
            PumpUntil(() => Results()?.ItemCount > 0);
            ShotOf(search, "search");
            Assert.That(Results()?.Items.OfType<IOLib.IAbstractFileStructure>().Select(f => f.Name), Is.EqualTo(new[] { "file.txt" }));
        }

        // Ctrl+P: Enter runs the selected plugin with the panel's folder and selection.
        [AvaloniaTest]
        public void Plugins_EnterRunsSelectedPlugin()
        {
            Open(_root, _dest);
            var plugins = new FakePluginManager("Sample");
            var window = new PluginWindow(plugins, _root, new[] { Path.Combine(_root, "file.txt") }, new ComplexLogger(_gui));
            window.Show(_window);
            Pump();
            window.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Pump();
            ShotOf(window, "plugins");

            Assert.That(plugins.Invoked, Is.EqualTo("Sample"));
        }

        private class FakePluginManager : IPluginManager
        {
            private readonly string _name;
            public string Invoked;
            public FakePluginManager(string name) => _name = name;
            public void LoadPlugins(string path) { }
            public System.Collections.Generic.IEnumerable<string> GetMethods() => new[] { _name };
            public Task InvokeMethod(string name, string dir, System.Collections.Generic.IEnumerable<string> files)
            {
                Invoked = name;
                return Task.CompletedTask;
            }
        }

        [AvaloniaTest]
        public void ConfirmAndConflictDialogs_KeyboardAnswers()
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

            var conflict = new IOLib.Conflict { SourcePath = Path.Combine(_root, "file.txt"), TargetPath = Path.Combine(_dest, "file.txt"), Index = 1 };
            var skip = new ConflictDialog(conflict);
            var skipResult = skip.ShowDialog<IOLib.ConflictAnswer>(_window);
            Pump();
            skip.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Pump();
            Assert.That(skipResult.IsCompleted, Is.True, "Escape closes the conflict dialog");
            Assert.That(skipResult.Result.Decision, Is.EqualTo(IOLib.ConflictDecision.Skip), "Esc skips only this file");

            var replace = new ConflictDialog(conflict);
            var replaceResult = replace.ShowDialog<IOLib.ConflictAnswer>(_window);
            Pump();
            replace.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Pump();
            Assert.That(replaceResult.Result.Decision, Is.EqualTo(IOLib.ConflictDecision.Replace), "Enter replaces");
        }

        private void CreateDotEntries()
        {
            Directory.CreateDirectory(Path.Combine(_root, ".git"));
            File.WriteAllText(Path.Combine(_root, ".env"), "");
        }

        private void AssertDotEntriesHidden(bool hidden)
        {
            PumpUntil(() => RowNames(1).Contains(".git") != hidden);
            if (hidden)
                Assert.That(RowNames(1), Has.None.EqualTo(".git").And.None.EqualTo(".env"));
            else
                Assert.That(RowNames(1), Is.SupersetOf(new[] { ".git", ".env" }));
            Assert.That(RowNames(1), Is.SupersetOf(new[] { "..", "file.txt", "child" }), "parent and normal entries stay");
        }

        [AvaloniaTest]
        public void CtrlH_HidesDotEntries_AndShowsThemAgain()
        {
            CreateDotEntries();
            Open(_root, _dest);
            AssertDotEntriesHidden(false);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.H, RawInputModifiers.Control);
            AssertDotEntriesHidden(true);
            Shot("hide-hidden");
            Key(Avalonia.Input.Key.H, RawInputModifiers.Control);
            AssertDotEntriesHidden(false);
        }

        [AvaloniaTest]
        public void ShowHiddenEye_ActsOnActivePanelOnly()
        {
            CreateDotEntries();
            Open(_root, _root);
            ClickRow(1, "file.txt");
            var eye = _window.GetVisualDescendants().OfType<ToggleButton>().First(t => t.Name == "ShowHiddenToggle");
            Assert.That(eye.IsChecked, Is.True, "hidden files are shown by default");
            Click(eye);
            AssertDotEntriesHidden(true);
            Assert.That(eye.IsChecked, Is.False);
            Assert.That(_vm.Io2.HideHidden, Is.False, "the other panel keeps its own setting");
        }

        [AvaloniaTest]
        public void Error_ShowsShortRedMessage_AndLogPanelListsIt()
        {
            Open(_root, _dest);
            _gui.Error("Cannot copy a.txt to /dst", new IOException("Access denied"));
            var status = _window.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "StatusMessage");
            PumpUntil(() => status.Text?.Contains("Access denied") == true);
            Shot("error");
            Assert.That(status.Text, Does.EndWith("Cannot copy a.txt to /dst: Access denied"));
            Assert.That(status.Classes, Does.Contain("error"));
            Assert.That(ToolTip.GetTip(status), Is.EqualTo("Cannot copy a.txt to /dst: Access denied"), "full text on hover");

            var logs = _window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "LogsButton");
            Assert.That(logs.Classes, Does.Contain("errors"), "unseen error turns the log button into the error chip");
            Assert.That(_vm.LogButtonText, Is.EqualTo("1 error · View log"));
            Click(logs);
            Shot("logs");
            Assert.That(logs.Flyout.IsOpen, Is.True, "Logs opens the log panel");
            var list = ((Control)((Flyout)logs.Flyout).Content).GetLogicalDescendants().OfType<ListBox>().Single(l => l.Name == "LogList");
            Assert.That(list.Items.OfType<Commander.Core.LogMsg>().Select(m => m.Message),
                Does.Contain("Cannot copy a.txt to /dst: Access denied"));
            Assert.That(_vm.ErrorCount, Is.Zero, "opening the log clears the chip");
            Assert.That(logs.Classes, Does.Not.Contain("errors"));
            logs.Flyout.Hide();
            Pump();
        }
    
        [AvaloniaTest]
        public void BackAndForward_ReturnToVisitedFolders()
        {
            Open(_root, _dest);
            var child = Path.Combine(_root, "child");
            ClickRow(1, "child");
            Key(Avalonia.Input.Key.Enter);
            PumpUntil(() => _vm.Io1.ActiveTab.Path == child);
            Assert.That(_vm.Io1.BackCommand.CanExecute(null), Is.True);
            Key(Avalonia.Input.Key.Left, RawInputModifiers.Alt);
            PumpUntil(() => _vm.Io1.ActiveTab.Path == _root);
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(_root), "Alt+Left goes back");
            Key(Avalonia.Input.Key.Right, RawInputModifiers.Alt);
            PumpUntil(() => _vm.Io1.ActiveTab.Path == child);
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(child), "Alt+Right goes forward");
            Assert.That(_vm.Io1.ForwardCommand.CanExecute(null), Is.False);
        }

        [AvaloniaTest]
        public void Tabs_KeepTheirOwnFolder()
        {
            Open(_root, _dest);
            var tabsBefore = _vm.Io1.Tabs.Count;
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.T, RawInputModifiers.Control);
            Assert.That(_vm.Io1.Tabs.Count, Is.EqualTo(tabsBefore + 1), "Ctrl+T opens a tab");
            var first = _vm.Io1.Tabs.First(t => t != _vm.Io1.ActiveTab && t.Path == _root);
            ClickRow(1, "child");
            Key(Avalonia.Input.Key.Enter);
            PumpUntil(() => _vm.Io1.ActiveTab.Path.EndsWith("child"));
            Shot("tabs");
            Assert.That(first.Path, Is.EqualTo(_root), "the other tab stays where it was");

            var firstButton = Named<ItemsControl>(1, "TabStrip").GetVisualDescendants().OfType<Button>()
                .First(b => b.DataContext == first);
            Click(firstButton);
            PumpUntil(() => _vm.Io1.ActualPath == _root);
            Assert.That(_vm.Io1.ActiveTab, Is.SameAs(first), "clicking a tab switches to it");

            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.W, RawInputModifiers.Control);
            Assert.That(_vm.Io1.Tabs.Count, Is.EqualTo(tabsBefore), "Ctrl+W closes the tab");
        }

        [AvaloniaTest]
        public void ActivePane_HasAccentBorder_AndFollowsTab()
        {
            Open(_root, _root);
            ClickRow(1, "file.txt");
            Assert.That(Named<Border>(1, "Card").Classes, Does.Contain("active"));
            Assert.That(Named<Border>(2, "Card").Classes, Does.Not.Contain("active"));
            Key(Avalonia.Input.Key.Tab);
            Shot("active-pane");
            Assert.That(Named<Border>(1, "Card").Classes, Does.Not.Contain("active"));
            Assert.That(Named<Border>(2, "Card").Classes, Does.Contain("active"));
        }

        [AvaloniaTest]
        public void Breadcrumb_NavigatesToParent_AndFooterCounts()
        {
            var child = Path.Combine(_root, "child");
            Open(child, _dest);
            PumpUntil(() => _vm.Io1.Segments.LastOrDefault()?.Name == "child");
            var crumb = Named<ItemsControl>(1, "Breadcrumb").GetVisualDescendants().OfType<Button>()
                .First(b => (b.DataContext as Commander.ViewModel.PathSegment)?.FullPath == _root);
            Click(crumb);
            PumpUntil(() => RowNames(1).Contains("file.txt"));
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(_root));
            Shot("pane");
            Assert.That(Named<TextBlock>(1, "SummaryText").Text, Is.EqualTo("2 folders · 2 files"));
            Assert.That(_vm.Io1.SpaceText, Does.Contain(" free of "));
        }
    
        [AvaloniaTest]
        public void CtrlK_Palette_RunsCommand_AndJumpsToFile()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            var tabs = _vm.Io1.Tabs.Count;
            Key(Avalonia.Input.Key.K, RawInputModifiers.Control);
            Assert.That(_vm.Palette.IsOpen, Is.True, "Ctrl+K opens the palette");
            var box = _window.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == "PaletteBox");
            Assert.That(box.IsFocused, Is.True);
            Type("new tab");
            Shot("palette");
            Assert.That(_vm.Palette.Selected?.Label, Is.EqualTo("New tab"));
            Key(Avalonia.Input.Key.Enter);
            Assert.That(_vm.Palette.IsOpen, Is.False);
            Assert.That(_vm.Io1.Tabs.Count, Is.EqualTo(tabs + 1), "Enter ran the command on the active panel");

            PumpUntil(() => RowNames(1).Contains("notes.md"));
            Key(Avalonia.Input.Key.K, RawInputModifiers.Control);
            Type("notes");
            Key(Avalonia.Input.Key.Enter);
            Assert.That(_vm.Io1.SelectedFile?.Name, Is.EqualTo("notes.md"), "a file result moves the cursor to it");
            Key(Avalonia.Input.Key.K, RawInputModifiers.Control);
            Key(Avalonia.Input.Key.Escape);
            Assert.That(_vm.Palette.IsOpen, Is.False, "Esc closes");
        }

        [AvaloniaTest]
        public void ErrorAfterClearLog_DoesNotThrow()
        {
            // The old "Index was out of range": stepping the log list right after Clear read item 0 of an empty list.
            Open(_root, _dest);
            _vm.ClearLogCommand.Execute(null);
            _gui.Error("Cannot open x");
            PumpUntil(() => _vm.CurrentLogMsg != null);
            Assert.That(_vm.LogsCollection, Has.Count.EqualTo(1));
            Assert.That(_vm.ErrorCount, Is.EqualTo(1));
        }
    
        [AvaloniaTest]
        public void CopyOntoExistingFile_ConflictDialog_KeepBoth()
        {
            File.WriteAllText(Path.Combine(_dest, "file.txt"), "old");
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.F5);
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
            var conflict = Dialog<ConflictDialog>();
            ShotOf(conflict, "conflict-window");
            ClickIn(conflict, conflict.GetVisualDescendants().OfType<Button>().First(b => b.Name == "KeepBothButton"));
            PumpUntil(() => File.Exists(Path.Combine(_dest, "file (1).txt")));
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("old"));
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file (1).txt")), Is.EqualTo("hello"));
        }

        [AvaloniaTest]
        public void F8_MovesToTrash_ShiftF8_DeletesPermanently()
        {
            if (OperatingSystem.IsWindows()) Assert.Ignore("Recycle Bin is not redirected in tests");
            var trash = Directory.CreateDirectory(Path.Combine(_root, "trash-under-test")).FullName;
            IOLib.Trash.MacTrashDir = trash;
            IOLib.Trash.LinuxTrashDir = trash;
            try
            {
                Open(_root, _dest);
                ClickRow(1, "file.txt");
                Key(Avalonia.Input.Key.F8);
                var dialog = Dialog<DeleteDialog>();
                Shot("delete");
                Assert.That(dialog.Classes, Does.Not.Contain("danger"));
                Answer<DeleteDialog>(Avalonia.Input.Key.Enter);
                PumpUntil(() => !File.Exists(Path.Combine(_root, "file.txt")));
                Assert.That(Directory.GetFiles(trash, "file.txt", SearchOption.AllDirectories), Has.Length.EqualTo(1), "F8 goes to the Trash");

                PumpUntil(() => RowNames(1).Contains("notes.md"));
                ClickRow(1, "notes.md");
                Key(Avalonia.Input.Key.F8, RawInputModifiers.Shift);
                var permanent = Dialog<DeleteDialog>();
                Shot("delete-permanent");
                Assert.That(permanent.Classes, Does.Contain("danger"), "permanent mode turns the dialog red");
                Answer<DeleteDialog>(Avalonia.Input.Key.Enter);
                PumpUntil(() => !File.Exists(Path.Combine(_root, "notes.md")));
                Assert.That(Directory.GetFiles(trash, "notes.md", SearchOption.AllDirectories), Is.Empty, "Shift+F8 skips the Trash");
            }
            finally
            {
                IOLib.Trash.MacTrashDir = null;
                IOLib.Trash.LinuxTrashDir = null;
            }
        }
    
        [AvaloniaTest]
        public void ShiftF2_CompareDirs_ListsDifferences_AndSynchronizes()
        {
            File.WriteAllText(Path.Combine(_dest, "only-right.txt"), "r");
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.F2, RawInputModifiers.Shift);
            var window = Dialog<CompareDirsWindow>();
            var vm = (Commander.ViewModel.CompareVM)window.DataContext;
            PumpUntil(() => vm.Rows.Count > 0);
            ShotOf(window, "compare-window");
            Assert.That(vm.Rows.Single(r => r.RightName == "only-right.txt").ToLeft, Is.True);
            Assert.That(vm.Rows.Single(r => r.LeftName == "file.txt").ToRight, Is.True);
            Assert.That(vm.PlanText, Does.StartWith("Plan: 4 → right · 1 ← left"));

            vm.FilterIndex = 3;
            Assert.That(vm.Rows.Select(r => r.RightName), Is.EqualTo(new[] { "only-right.txt" }), "Only right chip");

            vm.SyncCommand.Execute(null);
            Answer<ConfirmWindow>(Avalonia.Input.Key.Enter);
            // 30 s: slow CI runners (macOS) can take longer than the 5 s default to finish the sync job.
            PumpUntil(() => File.Exists(Path.Combine(_root, "only-right.txt")) && File.Exists(Path.Combine(_dest, "notes.md"))
                            && File.Exists(Path.Combine(_dest, "file.txt")), 30000);
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("hello"));
            Assert.That(Directory.Exists(Path.Combine(_dest, "child")), Is.True, "folders only on the left are copied too");
            window.Close();
        }
    
        [AvaloniaTest]
        public void ShiftF3_FileDiff_ShowsDifferences_EditsAndSaves()
        {
            File.WriteAllText(Path.Combine(_dest, "file.txt"), "hallo\nextra\n");
            Open(_root, _dest);
            ClickRow(2, "file.txt");
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.F3, RawInputModifiers.Shift);
            var window = Dialog<FileDiffWindow>();
            var vm = (Commander.ViewModel.FileDiffVM)window.DataContext;
            PumpUntil(() => vm.Rows.Count > 0);
            ShotOf(window, "file-diff");
            Assert.That(vm.Rows.Select(r => r.Kind), Is.EqualTo(new[] { IOLib.DiffKind.Changed, IOLib.DiffKind.OnlyRight }));
            Assert.That(vm.Rows[0].LeftSpans, Is.EqualTo(new[] { (1, 1) }), "only the differing letter is highlighted");
            var line = window.GetVisualDescendants().OfType<Commander.Controls.DiffLine>().First(l => l.Line == "hello");
            Assert.That(((Avalonia.Controls.Documents.Run)line.Inlines[1]).Background, Is.Not.Null, "the differing letter has a background");

            void Press(Key key, RawInputModifiers mods = RawInputModifiers.None)
            {
                window.KeyPress(key, mods, PhysicalKey.None, null);
                window.KeyRelease(key, mods, PhysicalKey.None, null);
                Pump();
            }

            // Edit the left line in place, then undo it.
            Press(Avalonia.Input.Key.Enter);
            Assert.That(vm.IsEditing, Is.True);
            window.KeyTextInput("!");
            Pump();
            Press(Avalonia.Input.Key.Escape);
            Assert.That(vm.IsEditing, Is.False);
            Assert.That(window.IsVisible, Is.True, "Esc while editing ends the edit, not the window");
            Assert.That(vm.Rows[0].Left, Is.EqualTo("hello!"));
            Press(Avalonia.Input.Key.Z, RawInputModifiers.Control);
            Assert.That(vm.Rows[0].Left, Is.EqualTo("hello"));

            // Copy the difference to the right and save.
            Press(Avalonia.Input.Key.Right, RawInputModifiers.Alt);
            Press(Avalonia.Input.Key.S, RawInputModifiers.Control);
            PumpUntil(() => !vm.HasChanges);
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("hello\n"));
            Assert.That(File.ReadAllText(Path.Combine(_root, "file.txt")), Is.EqualTo("hello"), "the left file is untouched");
            window.Close();
        }

        private static void PressIn(Window window, Key key, RawInputModifiers mods = RawInputModifiers.None)
        {
            window.KeyPress(key, mods, PhysicalKey.None, null);
            window.KeyRelease(key, mods, PhysicalKey.None, null);
            Pump();
        }

        private (FileDiffWindow Window, Commander.ViewModel.FileDiffVM Vm) OpenFileDiff()
        {
            Key(Avalonia.Input.Key.F3, RawInputModifiers.Shift);
            var window = (FileDiffWindow)Dialog<FileDiffWindow>();
            var vm = (Commander.ViewModel.FileDiffVM)window.DataContext;
            PumpUntil(() => vm.Rows.Count > 0);
            return (window, vm);
        }

        [AvaloniaTest] // T16
        public void ShiftF3_TwoMarkedFiles_ComparesThoseTwo()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.S);
            ClickRow(1, "notes.md");
            Key(Avalonia.Input.Key.S);
            var (window, vm) = OpenFileDiff();
            Assert.That(vm.LeftPath, Is.EqualTo(Path.Combine(_root, "file.txt")));
            Assert.That(vm.RightPath, Is.EqualTo(Path.Combine(_root, "notes.md")));
            window.Close();
        }

        [AvaloniaTest] // T17
        public void ShiftF3_SameFileInBothPanels_LogsAnError_AndOpensNothing()
        {
            Open(_root, _root);
            ClickRow(2, "file.txt");
            ClickRow(1, "file.txt");
            Key(Avalonia.Input.Key.F3, RawInputModifiers.Shift);
            PumpUntil(() => _vm.LogsCollection.Any(l => l.Message.Contains("Compare files")));
            Assert.That(_vm.LogsCollection.Any(l => l.Message.Contains("Compare files")), Is.True, "error logged");
            Assert.That(_window.OwnedWindows.OfType<FileDiffWindow>(), Is.Empty);
        }

        [AvaloniaTest] // T18
        public void ShiftF3_OneMarkedFile_ComparesTheCursorFiles()
        {
            File.WriteAllText(Path.Combine(_dest, "file.txt"), "hallo");
            Open(_root, _dest);
            ClickRow(2, "file.txt");
            ClickRow(1, "notes.md");
            Key(Avalonia.Input.Key.S);
            ClickRow(1, "file.txt");
            var (window, vm) = OpenFileDiff();
            Assert.That(vm.LeftPath, Is.EqualTo(Path.Combine(_root, "file.txt")));
            Assert.That(vm.RightPath, Is.EqualTo(Path.Combine(_dest, "file.txt")));
            window.Close();
        }

        [AvaloniaTest] // T30
        public void FileDiff_CloseWithUnsavedEdit_Asks_NoKeepsItOpen_YesDiscards()
        {
            File.WriteAllText(Path.Combine(_dest, "file.txt"), "hallo");
            Open(_root, _dest);
            ClickRow(2, "file.txt");
            ClickRow(1, "file.txt");
            var (window, vm) = OpenFileDiff();
            PressIn(window, Avalonia.Input.Key.Enter);
            window.KeyTextInput("!");
            Pump();
            PressIn(window, Avalonia.Input.Key.Escape);
            Assert.That(vm.HasChanges, Is.True);

            window.Close();
            Answer<ConfirmWindow>(Avalonia.Input.Key.Escape);
            Assert.That(window.IsVisible, Is.True, "No keeps the window and the edit");
            Assert.That(vm.HasChanges, Is.True);

            window.Close();
            Answer<ConfirmWindow>(Avalonia.Input.Key.Enter);
            PumpUntil(() => !window.IsVisible);
            Assert.That(window.IsVisible, Is.False);
            Assert.That(File.ReadAllText(Path.Combine(_root, "file.txt")), Is.EqualTo("hello"), "nothing saved");
        }

        [AvaloniaTest] // T31
        public void FileDiff_EditingKeys_JoinSplitDownTabUndoEsc()
        {
            File.WriteAllText(Path.Combine(_root, "a.txt"), "one\ntwo\nthree\n");
            File.WriteAllText(Path.Combine(_dest, "a.txt"), "one\nTWO\nthree\n");
            Open(_root, _dest);
            ClickRow(2, "a.txt");
            ClickRow(1, "a.txt");
            var (window, vm) = OpenFileDiff();
            Assert.That(vm.CursorIndex, Is.EqualTo(1), "starts on the difference");
            string LeftText() => string.Join("|", vm.Rows.Where(r => r.LeftNo >= 0).Select(r => r.Left));

            PressIn(window, Avalonia.Input.Key.F2);
            Assert.That(vm.IsEditing, Is.True, "F2 edits");
            PressIn(window, Avalonia.Input.Key.Home);
            PressIn(window, Avalonia.Input.Key.Back);
            Assert.That(LeftText(), Is.EqualTo("onetwo|three"), "Backspace at the start joins");
            PressIn(window, Avalonia.Input.Key.Enter);
            Assert.That(LeftText(), Is.EqualTo("one|two|three"), "Enter splits at the caret");
            PressIn(window, Avalonia.Input.Key.Down);
            Assert.That(vm.IsEditing, Is.True, "Down keeps editing");
            Assert.That(vm.Current.LeftNo, Is.EqualTo(2));
            PressIn(window, Avalonia.Input.Key.Escape);
            Assert.That(vm.IsEditing, Is.False);
            Assert.That(window.IsVisible, Is.True);

            PressIn(window, Avalonia.Input.Key.Tab);
            Assert.That(vm.ActiveSide, Is.EqualTo(1), "Tab switches side");
            PressIn(window, Avalonia.Input.Key.Z, RawInputModifiers.Meta);
            Assert.That(LeftText(), Is.EqualTo("onetwo|three"), "Cmd+Z undoes the split");
            PressIn(window, Avalonia.Input.Key.Z, RawInputModifiers.Control);
            Assert.That(LeftText(), Is.EqualTo("one|two|three"));
            Assert.That(vm.HasChanges, Is.False);

            // No KeyRelease: Esc closes the window on the press.
            window.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
            PumpUntil(() => !window.IsVisible);
            Assert.That(window.IsVisible, Is.False, "Esc when not editing closes");
        }

        [AvaloniaTest] // T32
        public void FileDiff_ClickOnTheRightHalf_EditsTheRightSide()
        {
            File.WriteAllText(Path.Combine(_dest, "file.txt"), "hallo");
            Open(_root, _dest);
            ClickRow(2, "file.txt");
            ClickRow(1, "file.txt");
            var (window, vm) = OpenFileDiff();
            var list = window.FindControl<ListBox>("RowList");
            var row = (Control)list.ContainerFromIndex(0);
            var p = row.TranslatePoint(new Point(row.Bounds.Width * 0.75, row.Bounds.Height / 2), window).Value;
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            Pump();
            Assert.That(vm.ActiveSide, Is.EqualTo(1));
            PressIn(window, Avalonia.Input.Key.F2);
            Assert.That(vm.Current.IsEditingRight, Is.True);
            PressIn(window, Avalonia.Input.Key.Escape);
            window.Close();
        }

        [AvaloniaTest]
        public void ShiftF3_OnAFolder_LogsAnError_AndOpensNothing()
        {
            Open(_root, _dest);
            ClickRow(1, "child");
            Key(Avalonia.Input.Key.F3, RawInputModifiers.Shift);
            Pump();
            Assert.That(_window.OwnedWindows.OfType<FileDiffWindow>(), Is.Empty);
        }

        [AvaloniaTest]
        public void Settings_AccentAppliesLive_ShortcutRecording_AndCancelReverts()
        {
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            var accentBefore = AppSettings.Current.Accent;
            Key(Avalonia.Input.Key.OemComma, RawInputModifiers.Control);
            var window = (SettingsWindow)Dialog<SettingsWindow>();
            var vm = (Commander.ViewModel.SettingsWindowVM)window.DataContext;

            vm.SetAccentCommand.Execute("#F0A04B");
            Pump();
            Application.Current.TryGetResource("Brush.Accent", Avalonia.Styling.ThemeVariant.Dark, out var brush);
            Assert.That(((Avalonia.Media.ISolidColorBrush)brush).Color, Is.EqualTo(Avalonia.Media.Color.Parse("#F0A04B")), "accent applies live");
            ShotOf(window, "settings");

            vm.Section = 1;
            var copyRow = vm.Groups.SelectMany(g => g.Rows).First(r => r.Command.Id == "copy");
            vm.RecordCommand.Execute(copyRow);
            Assert.That(vm.RecordKey(Avalonia.Input.Key.F6, Avalonia.Input.KeyModifiers.None), Is.True);
            Assert.That(vm.ConflictText, Does.Contain("Move to other pane"), "F6 belongs to Move");
            vm.ReassignCommand.Execute(null);
            Assert.That(_vm.KeyMap["copy"].ToString(), Is.EqualTo("F6"));
            Assert.That(_vm.KeyMap.GesturesOf("move").Select(g => g.ToString()), Does.Not.Contain("F6"));
            Pump();
            ShotOf(window, "settings-keyboard");

            window.Close();
            Pump();
            Assert.That(AppSettings.Current.Accent, Is.EqualTo(accentBefore), "closing without Done reverts");
            Assert.That(_vm.KeyMap["copy"].ToString(), Is.EqualTo("F5"));
        }
    
        // T12: Help → Check for updates and the palette both reach the update command (never run here: it calls GitHub).
        [AvaloniaTest]
        public void Help_CheckForUpdates_IsInMenuAndPalette()
        {
            Open(_root, _dest);
            var help = _window.GetLogicalDescendants().OfType<MenuItem>().First(m => m.Header as string == "Help");
            var item = help.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header as string == "Check for updates");
            Assert.That(item, Is.Not.Null, "menu item");
            Assert.That(item.Command, Is.SameAs(_vm.CheckUpdatesCommand), "menu command bound");
            Assert.That(CommandRegistry.Get("checkUpdates").Resolve(_vm, _vm.ActivePanel), Is.SameAs(_vm.CheckUpdatesCommand), "palette command");
        }

        // T13: the Tools page checkbox drives CheckForUpdates, and closing without Done reverts it.
        [AvaloniaTest]
        public void Settings_UpdateCheckbox_CancelReverts()
        {
            Open(_root, _dest);
            var before = AppSettings.Current.CheckForUpdates;
            _vm.SettingsCommand.Execute(null);
            var window = (SettingsWindow)Dialog<SettingsWindow>();
            ((Commander.ViewModel.SettingsWindowVM)window.DataContext).Section = 3;
            Pump();
            var box = window.FindControl<CheckBox>("CheckUpdates");
            Assert.That(box.IsChecked, Is.EqualTo(before));
            ClickIn(window, box);
            Assert.That(AppSettings.Current.CheckForUpdates, Is.EqualTo(!before), "the checkbox drives the setting");
            window.Close();   // never Done: it writes the real Config.xml
            Pump();
            Assert.That(AppSettings.Current.CheckForUpdates, Is.EqualTo(before), "closing without Done reverts");
        }

        [AvaloniaTest]
        public void Settings_ToolsPage_PresetFillsCommand_ResetClears_CancelReverts()
        {
            Open(_root, _dest);
            var before = AppSettings.Current.TerminalCommand;
            _vm.SettingsCommand.Execute(null);
            var window = (SettingsWindow)Dialog<SettingsWindow>();
            var vm = (Commander.ViewModel.SettingsWindowVM)window.DataContext;
            vm.Section = 3;
            Pump();
            Assert.That(vm.IsTools, Is.True);
            Assert.That(vm.TerminalPlaceholder, Is.Not.Empty, "shows the platform default");
            Assert.That(vm.TerminalPresets, Is.Not.Empty);

            vm.SelectedTerminalPreset = vm.TerminalPresets[0];
            Pump();
            var box = window.FindControl<TextBox>("TerminalCommand");
            Assert.That(box.Text, Is.EqualTo(vm.TerminalPresets[0].Value), "a preset fills the text box");
            ShotOf(window, "settings-tools");

            vm.ClearToolCommand.Execute("terminal");
            Pump();
            Assert.That(AppSettings.Current.TerminalCommand, Is.Empty, "Reset goes back to the default");

            vm.SelectedTerminalPreset = vm.TerminalPresets[0];
            window.Close();   // never Done: it writes the real Config.xml
            Pump();
            Assert.That(AppSettings.Current.TerminalCommand, Is.EqualTo(before), "closing without Done reverts");
        }

        [AvaloniaTest]
        public void HelpKeyboardShortcuts_OpensSettingsOnKeyboardPage()
        {
            Open(_root, _dest);
            _vm.KeyMapCommand.Execute(null);
            var window = (SettingsWindow)Dialog<SettingsWindow>();
            var vm = (Commander.ViewModel.SettingsWindowVM)window.DataContext;
            Assert.That(vm.Section, Is.EqualTo(1));
            Assert.That(vm.IsKeyboard, Is.True);
            window.Close();   // never Done: it writes the real Config.xml
            Pump();
        }

        [AvaloniaTest]
        public void ThumbnailView_ShowsImages_AndKeysStillWork()
        {
            using (var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(16, 16)))
                bitmap.Save(Path.Combine(_root, "pic.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            Open(_root, _dest);
            ClickRow(1, "file.txt");
            var toggle = _window.GetVisualDescendants().OfType<ListBox>().First(l => l.Name == "ViewToggle");
            Click(toggle.GetVisualDescendants().OfType<ListBoxItem>().ElementAt(1));
            Assert.That(_vm.Io1.IsThumbnails, Is.True, "the toggle switches the active pane");
            Assert.That(_vm.Io2.IsThumbnails, Is.False);
            var list = Named<ListBox>(1, "ThumbList");
            Assert.That(list.IsVisible && !Grid(1).IsVisible, Is.True);
            PumpUntil(() => list.GetVisualDescendants().OfType<Commander.Controls.Thumbnail>().Any(t => t.HasImage));
            Shot("thumbnails");
            Assert.That(list.GetVisualDescendants().OfType<Commander.Controls.Thumbnail>().Single(t => t.HasImage).FilePath, Does.EndWith("pic.png"));

            var child = _vm.Io1.Files.First(f => f.Name == "child");
            Click(list.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.DataContext == child));
            Key(Avalonia.Input.Key.Enter);
            PumpUntil(() => _vm.Io1.ActualPath.EndsWith("child"));
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(Path.Combine(_root, "child")), "Enter opens the folder in thumbnail view too");
        }
    

        [AvaloniaTest]
        public void FolderTree_EnterOpensSelectedFolder_AndStaysOpen()
        {
            Open(_root, _dest);
            // Cursor on a file: the tree opens on the panel folder (on a folder or "..", it opens on that one).
            ClickRow(1, "file.txt");
            _vm.Io1.TreeWindowCommand.Execute(null);
            var tree = Dialog<TreeWindow>();
            var view = tree.GetVisualDescendants().OfType<TreeView>().First();
            var root = (Commander.ViewModel.File)view.SelectedItem;
            Assert.That(root?.Path, Is.EqualTo(_root), "the root node starts selected");
            var child = root.Files.First(f => f.IsDir);
            view.SelectedItem = child;
            Pump();
            ShotOf(tree, "flow-tree");
            tree.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            PumpUntil(() => _vm.Io1.ActualPath == child.Path);
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(child.Path));
            Assert.That(tree.IsVisible, Is.True, "the tree stays open for the next jump");
        }

        [AvaloniaTest]
        public void History_EnterGoesToSelectedEntry()
        {
            Open(_root, _dest);
            _vm.Io1.ActualPath = Path.Combine(_root, "child");
            Pump();
            _vm.Io1.HistoryCommand.Execute(null);
            var history = Dialog<HistoryWindow>();
            var list = history.GetVisualDescendants().OfType<ListBox>().First();
            var target = list.Items.Cast<string>().First(p => p != _vm.Io1.ActualPath);
            list.SelectedItem = target;
            Pump();
            ShotOf(history, "flow-history");
            Answer<HistoryWindow>(Avalonia.Input.Key.Enter);
            PumpUntil(() => _vm.Io1.ActualPath == target);
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(target));
        }

        // Enter, over and drop at the centre of target, like the OS does for one drag.
        private void DropOn(Control target, RawInputModifiers mods, params string[] paths) => DropOn(target, mods, null, paths);

        // beforeDrop runs after the drag picked up the items: deletes there act like Finder deleting the file mid-drag.
        private void DropOn(Control target, RawInputModifiers mods, Action beforeDrop, params string[] paths)
        {
            var p = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), _window).Value;
            var data = new DataTransfer();
            // Real storage items, as Finder/Explorer hands them over; one item per path.
            foreach (var path in paths)
            {
                var task = _window.StorageProvider.TryGetFileFromPathAsync(new Uri(path));
                PumpUntil(() => task.IsCompleted);
                Assert.That(task.Result, Is.Not.Null, "headless storage provider gave no item for " + path);
                data.Add(DataTransferItem.CreateFile(task.Result));
            }
            var all = DragDropEffects.Copy | DragDropEffects.Move;
            _window.DragDrop(p, Avalonia.Input.Raw.RawDragEventType.DragEnter, data, all, mods);
            _window.DragDrop(p, Avalonia.Input.Raw.RawDragEventType.DragOver, data, all, mods);
            beforeDrop?.Invoke();
            _window.DragDrop(p, Avalonia.Input.Raw.RawDragEventType.Drop, data, all, mods);
            Pump();
        }

        private string[] Errors() => _vm.LogsCollection.Where(l => l.IsError).Select(l => l.Message).ToArray();

        [AvaloniaTest]
        public void DropFile_OnOtherPane_OpensCopyDialog_AndCopies()
        {
            Open(_root, _dest);
            DropOn(Grid(2), RawInputModifiers.None, Path.Combine(_root, "file.txt"));
            var dialog = (CopyDialog)Dialog<CopyDialog>();
            Assert.That(dialog.FindControl<TextBlock>("TitleText").Text, Is.EqualTo("Copy 1 item"));
            Assert.That(dialog.FindControl<TextBox>("DestinationBox").Text, Is.EqualTo(_dest), "drops go into the folder of the pane under the mouse");
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
            PumpUntil(() => RowNames(2).Contains("file.txt"));
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("hello"));
            Assert.That(File.Exists(Path.Combine(_root, "file.txt")), Is.True, "copy keeps the source");
            Assert.That(_vm.ActivePanel, Is.SameAs(_vm.Io2), "the drop target pane takes focus");
        }

        [AvaloniaTest]
        public void ShiftDropFile_OnOtherPane_OpensMoveDialog_AndMoves()
        {
            Open(_root, _dest);
            DropOn(Grid(2), RawInputModifiers.Shift, Path.Combine(_root, "file.txt"));
            Assert.That(((CopyDialog)Dialog<CopyDialog>()).FindControl<TextBlock>("TitleText").Text, Is.EqualTo("Move 1 item"));
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
            PumpUntil(() => RowNames(2).Contains("file.txt"));
            Assert.That(File.ReadAllText(Path.Combine(_dest, "file.txt")), Is.EqualTo("hello"));
            Assert.That(File.Exists(Path.Combine(_root, "file.txt")), Is.False, "move removes the source");
        }

        [AvaloniaTest]
        public void DropFile_EscInDialog_ChangesNothing()
        {
            Open(_root, _dest);
            DropOn(Grid(2), RawInputModifiers.None, Path.Combine(_root, "file.txt"));
            Answer<CopyDialog>(Avalonia.Input.Key.Escape);
            Assert.That(File.Exists(Path.Combine(_dest, "file.txt")), Is.False);
            Assert.That(Errors(), Is.Empty);
        }

        [AvaloniaTest]
        public void DropFile_OnThumbnailView_Copies()
        {
            Open(_root, _dest);
            _vm.Io2.ViewIndex = 1;
            Pump();
            var list = Named<ListBox>(2, "ThumbList");
            Assert.That(list.IsVisible, Is.True);
            DropOn(list, RawInputModifiers.None, Path.Combine(_root, "notes.md"));
            Answer<CopyDialog>(Avalonia.Input.Key.Enter);
            PumpUntil(() => File.Exists(Path.Combine(_dest, "notes.md")));
            Assert.That(File.Exists(Path.Combine(_dest, "notes.md")), Is.True);
        }

        [AvaloniaTest]
        public void DropVanishedFile_LogsError_OpensNoDialog()
        {
            Open(_root, _dest);
            var gone = Path.Combine(_root, "gone.txt");
            File.WriteAllText(gone, "x");
            DropOn(Grid(2), RawInputModifiers.None, () => File.Delete(gone), gone);
            Pump();
            Assert.That(_window.OwnedWindows.OfType<CopyDialog>(), Is.Empty);
            Assert.That(Errors(), Has.Some.Contains("gone.txt").Or.Some.Contains("Drop failed"));
        }

        // Regression: the press handler now also starts drags; right double click must still mark the row.
        [AvaloniaTest]
        public void RightDoubleClick_StillMarksRow()
        {
            Open(_root, _dest);
            var p = Row(1, "file.txt").TranslatePoint(new Point(20, 8), _window).Value;
            for (int i = 0; i < 2; i++)
            {
                _window.MouseDown(p, MouseButton.Right);
                _window.MouseUp(p, MouseButton.Right);
            }
            Pump();
            Assert.That(_vm.Io1.Files.Single(f => f.Name == "file.txt").IsSelected(), Is.True);
        }

        // Dropping a drag back onto the pane it came from would copy files onto themselves: it must do nothing.
        [AvaloniaTest]
        public void MouseDragOfRow_DroppedOnSamePane_DoesNothing()
        {
            Open(_root, _dest);
            var p = Row(1, "file.txt").TranslatePoint(new Point(20, 8), _window).Value;
            _window.MouseDown(p, MouseButton.Left);
            _window.MouseMove(p + new Point(40, 0), RawInputModifiers.LeftMouseButton);
            Pump();
            DropOn(Grid(1), RawInputModifiers.None, Path.Combine(_root, "file.txt"));
            _window.MouseUp(p + new Point(40, 0), MouseButton.Left);
            Pump();
            Assert.That(_window.OwnedWindows.OfType<CopyDialog>(), Is.Empty);
            Assert.That(Errors(), Is.Empty);
        }

        // A real mouse drag of a row: past the 6 px threshold it starts a drag, and nothing breaks or copies on its own.
        [AvaloniaTest]
        public void MouseDragOfRow_DoesNotLogError_OrOpenDialog()
        {
            Open(_root, _dest);
            var p = Row(1, "file.txt").TranslatePoint(new Point(20, 8), _window).Value;
            _window.MouseDown(p, MouseButton.Left);
            _window.MouseMove(p + new Point(3, 0), RawInputModifiers.LeftMouseButton);
            _window.MouseMove(p + new Point(40, 0), RawInputModifiers.LeftMouseButton);
            Pump();
            _window.MouseUp(p + new Point(40, 0), MouseButton.Left);
            Pump();
            Assert.That(Errors(), Is.Empty);
            Assert.That(_window.OwnedWindows.OfType<CopyDialog>(), Is.Empty);
            // The pane still answers the keyboard afterwards.
            ClickRow(1, "child");
            Key(Avalonia.Input.Key.Enter);
            Assert.That(_vm.Io1.ActualPath, Is.EqualTo(Path.Combine(_root, "child")));
        }
    }
}
