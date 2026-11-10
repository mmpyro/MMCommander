using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Comander;
using Comander.CommanderIO;
using LogLib;
using NUnit.Framework;

namespace ComanderTest
{
    // Real input through the window: Enter and double click on a folder row must open it.
    [TestFixture]
    public class PanelNavigationTest
    {
        private string _root;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-nav-" + Guid.NewGuid().ToString("N"))).FullName;
            Directory.CreateDirectory(Path.Combine(_root, "child"));
            File.WriteAllText(Path.Combine(_root, "file.txt"), "");
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private static void Pump()
        {
            for (int i = 0; i < 20; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(20); }
        }

        private (Window window, Comander.ViewModel.MainVM vm, DataGrid grid, DataGridRow childRow) Open()
        {
            var vm = App.CreateMainVm(new ComplexLogger(new GUILogger()));
            var window = new MainWindow { DataContext = vm, Width = 1200, Height = 700 };
            window.Show();
            vm.Io1.ActualPath = _root;
            Pump();
            var grid = window.GetVisualDescendants().OfType<DataGrid>().First();
            var row = grid.GetVisualDescendants().OfType<DataGridRow>()
                .First(r => (r.DataContext as IMetadataFileStructure)?.Name == "child");
            return (window, vm, grid, row);
        }

        private static Point Centre(Window window, Control control) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window).Value;

        [AvaloniaTest]
        public void ClickThenEnter_OpensFolder()
        {
            var (window, vm, _, row) = Open();
            var p = Centre(window, row);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            Pump();
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Pump();
            Assert.That(vm.Io1.ActualPath, Is.EqualTo(Path.Combine(_root, "child")));
        }

        [AvaloniaTest]
        public void DoubleClick_OpensFolder()
        {
            var (window, vm, _, row) = Open();
            var p = Centre(window, row);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            window.MouseDown(p, MouseButton.Left);
            window.MouseUp(p, MouseButton.Left);
            Pump();
            Assert.That(vm.Io1.ActualPath, Is.EqualTo(Path.Combine(_root, "child")));
        }

        // Folders are listed with IsDirectory = true (a two-way column binding used to reset it) and styled as "dir".
        [AvaloniaTest]
        public void FolderRows_AreMarkedAsDirectories()
        {
            var (window, vm, grid, row) = Open();
            var dir = Environment.GetEnvironmentVariable("MMC_SCREENSHOTS");
            if (!string.IsNullOrEmpty(dir))
                window.CaptureRenderedFrame().Save(Path.Combine(dir, "Folders.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            Assert.That(vm.Io1.Files.Where(f => f.Name != "file.txt").Select(f => f.IsDirectory), Is.All.True);
            Assert.That(row.Classes.Contains("dir"), Is.True);
            var fileRow = grid.GetVisualDescendants().OfType<DataGridRow>().First(r => (r.DataContext as IMetadataFileStructure)?.Name == "file.txt");
            Assert.That(fileRow.Classes.Contains("dir"), Is.False);
        }
    }
}
