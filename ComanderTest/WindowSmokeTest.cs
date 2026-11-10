using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Comander;
using Comander.CommanderIO;
using Comander.Core;
using Comander.View;
using LogLib;
using NUnit.Framework;

namespace ComanderTest
{
    // Loads every window through its compiled XAML; catches binding/XAML errors that only show at runtime.
    // Set MMC_SCREENSHOTS=<dir> to save a PNG of each window.
    public class WindowSmokeTest
    {
        private static void ShowAndCapture(Window window, string name)
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var frame = window.CaptureRenderedFrame();
            Assert.That(frame, Is.Not.Null, name);
            var dir = Environment.GetEnvironmentVariable("MMC_SCREENSHOTS");
            if (!string.IsNullOrEmpty(dir))
                frame.Save(Path.Combine(dir, name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            window.Close();
        }

        [AvaloniaTest]
        public void MainWindow_Renders()
        {
            var window = new MainWindow { DataContext = App.CreateMainVm(new ComplexLogger(new GUILogger())) };
            window.Show();
            // Let the async folder listing finish before capturing.
            for (int i = 0; i < 20; i++) { Dispatcher.UIThread.RunJobs(); System.Threading.Thread.Sleep(50); }
            var frame = window.CaptureRenderedFrame();
            Assert.That(frame, Is.Not.Null);
            var dir = Environment.GetEnvironmentVariable("MMC_SCREENSHOTS");
            if (!string.IsNullOrEmpty(dir))
                frame.Save(Path.Combine(dir, "MainWindow.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        [AvaloniaTest]
        public void Dialogs_Render()
        {
            var file = (IMetadataFileStructure)new MetaDataFileFactory().CreateFileMsg(Path.Combine(AppContext.BaseDirectory, "Config.xml"));
            var logger = new ComplexLogger(new GUILogger());
            var windows = new Dictionary<string, Func<Window>>
            {
                ["About"] = () => new AboutWindow("Product version: test"),
                ["Confirm"] = () => new ConfirmWindow("Do you want delete 3 files ?"),
                ["Input"] = () => new InputWindow("New folder", "new folder"),
                ["Copy"] = () => new CopyDialog(new Comander.Dtos.CopyDialogModel
                {
                    Names = new[] { "image_1.png", "image_2.png", "image.png" }, TotalSize = 4_300_000, Destination = "/Users/Shared", FreeSpaceText = "412 GB free on Macintosh HD",
                }),
                ["Conflict"] = () => new ConflictDialog(new IOLib.Conflict
                {
                    SourcePath = file.FullName, TargetPath = "/tmp/Config.xml", SourceSize = 1_500_000, TargetSize = 1_400_000,
                    SourceTime = new DateTime(2024, 11, 27, 15, 37, 0), TargetTime = new DateTime(2024, 1, 2, 8, 29, 0), Index = 1,
                }),
                ["Delete"] = () => new DeleteDialog(DeleteModel(false)),
                ["DeletePermanent"] = () => new DeleteDialog(DeleteModel(true)),
                ["Progress"] = () => new ProgressDialog(new Comander.Core.TransferJob("Copying", "/Users/Shared", _ => { })),
                ["Zip"] = () => new ZipWindow(Path.GetTempPath()),
                ["UnZip"] = () => new UnZipWindow(file),
                ["Split"] = () => new SplitWindow(logger),
                ["Join"] = () => new JoinWindow(),
                ["History"] = () => new HistoryWindow(new[] { "/tmp", "/usr" }),
                ["Shortcuts"] = () => new ShortcutsWindow(new ShortcutManager()),
                // The panel is only used when a node is opened.
                ["Tree"] = () => new TreeWindow(AppContext.BaseDirectory, null),
                ["Info"] = () => new InfoWindow(file),
                ["Plugin"] = () => new PluginWindow(new NullPluginManager(), "/tmp", new string[0], logger),
                ["Search"] = () => new Search.SearchWindow(null, logger),
            };
            foreach (var (name, create) in windows)
                ShowAndCapture(create(), name);
        }

        private static Comander.Dtos.DeleteDialogModel DeleteModel(bool permanent) => new Comander.Dtos.DeleteDialogModel
        {
            Items = new[] { "image_1.png", "image_2.png", "image.png" }.Select(n => new Comander.Dtos.DeleteItem { Name = n, SizeText = "1.5 MB" }).ToList(),
            Folder = "/Users/me", TotalSizeText = "4.1 MB", Permanent = permanent,
        };
    }
}
