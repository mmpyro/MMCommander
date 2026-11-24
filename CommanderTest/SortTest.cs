using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Commander;
using IOManager = Commander.ViewModel.IOManager;
using MainVM = Commander.ViewModel.MainVM;
using LogLib;
using NUnit.Framework;

namespace CommanderTest
{
    // Panels sort by Name / Ext / Size / Modified; ".." stays first and folders stay before files.
    [TestFixture]
    public class SortTest
    {
        private string _root;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-sort-" + Guid.NewGuid().ToString("N"))).FullName;
            Directory.CreateDirectory(Path.Combine(_root, "b_dir"));
            Directory.CreateDirectory(Path.Combine(_root, "a_dir"));
            Write("big.txt", 100, -3);
            Write("small.log", 1, -1);
            Write("mid.md", 10, -2);
        }

        private void Write(string name, int size, int hoursAgo)
        {
            var path = Path.Combine(_root, name);
            File.WriteAllText(path, new string('x', size));
            File.SetLastWriteTime(path, DateTime.Now.AddHours(hoursAgo));
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private static void Pump()
        {
            for (int i = 0; i < 20; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(20); }
        }

        private static string[] Names(IOManager panel) => panel.Files.Select(f => f.Name).ToArray();

        private (Window window, MainVM vm) Open()
        {
            var vm = App.CreateMainVm(new ComplexLogger(new GUILogger()));
            var window = new MainWindow { DataContext = vm, Width = 1200, Height = 700 };
            window.Show();
            vm.Io1.ActualPath = _root;
            Pump();
            return (window, vm);
        }

        [AvaloniaTest]
        public void SortBy_EachColumn_KeepsParentAndFoldersFirst()
        {
            var (_, vm) = Open();
            var p = vm.Io1;
            Assert.That(Names(p), Is.EqualTo(new[] { "..", "a_dir", "b_dir", "big.txt", "mid.md", "small.log" }));

            p.SortBy("Size");
            Assert.That(Names(p), Is.EqualTo(new[] { "..", "a_dir", "b_dir", "small.log", "mid.md", "big.txt" }));
            p.SortBy("Size");
            Assert.That(p.SortDescending, Is.True);
            Assert.That(Names(p), Is.EqualTo(new[] { "..", "b_dir", "a_dir", "big.txt", "mid.md", "small.log" }));

            p.SortBy("Ext");
            Assert.That(p.SortDescending, Is.False);
            Assert.That(Names(p).Skip(3), Is.EqualTo(new[] { "small.log", "mid.md", "big.txt" }));

            p.SortBy("Modified");
            Assert.That(Names(p).Skip(3), Is.EqualTo(new[] { "big.txt", "mid.md", "small.log" }));

            p.SortBy("Name");
            p.SortBy("Name");
            Assert.That(Names(p), Is.EqualTo(new[] { "..", "b_dir", "a_dir", "small.log", "mid.md", "big.txt" }));
        }

        [AvaloniaTest]
        public void SortBy_KeepsMarksAndSurvivesRefresh()
        {
            var (_, vm) = Open();
            var p = vm.Io1;
            p.Files.First(f => f.Name == "mid.md").PermanentSelect();
            p.SortBy("Size");
            Assert.That(p.Files.First(f => f.Name == "mid.md").IsSelected(), Is.True);
            p.FilterFiles();
            Pump();
            Assert.That(Names(p).Skip(3), Is.EqualTo(new[] { "small.log", "mid.md", "big.txt" }));
        }

        [AvaloniaTest]
        public void HeaderClick_SortsPanelAndShowsArrow()
        {
            var (window, vm) = Open();
            var header = window.GetVisualDescendants().OfType<DataGridColumnHeader>().First(h => Equals(h.Content, "SIZE"));
            var at = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), window).Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Pump();
            Assert.That(vm.Io1.SortColumn, Is.EqualTo("Size"));
            Assert.That(Names(vm.Io1).Skip(3), Is.EqualTo(new[] { "small.log", "mid.md", "big.txt" }));
            var headers = window.GetVisualDescendants().OfType<DataGridColumnHeader>().Select(h => h.Content as string).ToList();
            Assert.That(headers, Does.Contain("SIZE ↑"));
            Assert.That(headers, Does.Contain("NAME"));
        }

        [AvaloniaTest]
        public void CtrlF5_SortsBySize_SecondPressReverses()
        {
            var (window, vm) = Open();
            var row = window.GetVisualDescendants().OfType<DataGridRow>()
                .First(r => (r.DataContext as Commander.CommanderIO.IMetadataFileStructure)?.Name == "mid.md");
            var at = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window).Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Pump();
            window.KeyPress(Key.F5, RawInputModifiers.Control, PhysicalKey.F5, null);
            Pump();
            Assert.That(vm.Io1.SortColumn, Is.EqualTo("Size"));
            Assert.That(Names(vm.Io1).Skip(3), Is.EqualTo(new[] { "small.log", "mid.md", "big.txt" }));
            window.KeyPress(Key.F5, RawInputModifiers.Control, PhysicalKey.F5, null);
            Pump();
            Assert.That(vm.Io1.SortDescending, Is.True);
            Assert.That(Names(vm.Io1).Skip(3), Is.EqualTo(new[] { "big.txt", "mid.md", "small.log" }));
            Assert.That(vm.Io2.SortColumn, Is.EqualTo("Name"), "the other panel keeps its own sort");
        }

        [AvaloniaTest]
        public void Sort_IsKeptWhenEnteringAnotherFolder()
        {
            // Name order is y, z; descending size must give z, y.
            File.WriteAllText(Path.Combine(_root, "a_dir", "y.txt"), "x");
            File.WriteAllText(Path.Combine(_root, "a_dir", "z.txt"), new string('x', 50));
            var (_, vm) = Open();
            vm.Io1.SortBy("Size");
            vm.Io1.SortBy("Size");
            vm.Io1.ActualPath = Path.Combine(_root, "a_dir");
            Pump();
            Assert.That(Names(vm.Io1), Is.EqualTo(new[] { "..", "z.txt", "y.txt" }));
        }

        [AvaloniaTest]
        public void ExtSort_NoExtensionFirst_SameExtensionByName()
        {
            Write("Makefile", 5, -4);
            Write("a.txt", 5, -4);
            var (_, vm) = Open();
            vm.Io1.SortBy("Ext");
            Assert.That(Names(vm.Io1).Skip(3), Is.EqualTo(new[] { "Makefile", "small.log", "mid.md", "a.txt", "big.txt" }));
        }

        [AvaloniaTest]
        public void SortBy_KeepsCursorOnSameFile()
        {
            var (_, vm) = Open();
            var mid = vm.Io1.Files.First(f => f.Name == "mid.md");
            vm.Io1.SelectedFile = mid;
            vm.Io1.SortBy("Modified");
            Pump();
            Assert.That(vm.Io1.SelectedFile, Is.SameAs(mid));
        }
    }
}
