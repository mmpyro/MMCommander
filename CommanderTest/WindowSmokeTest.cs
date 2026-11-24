using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Commander;
using Commander.CommanderIO;
using Commander.Core;
using Commander.View;
using LogLib;
using NUnit.Framework;

namespace CommanderTest
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

        [Test]
        public void About_ShowsProductVersion()
        {
            Assert.That(new Commander.Core.AssemblyVersionResolver().GetProductVersion(typeof(Commander.ViewModel.MainVM)), Is.EqualTo("2.2.0"));
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
                ["About"] = () => new AboutWindow("1.2.3"),
                ["Confirm"] = () => new ConfirmWindow("Do you want delete 3 files ?"),
                ["Input"] = () => new InputWindow("New folder", "new folder"),
                ["Copy"] = () => new CopyDialog(new Commander.Dtos.CopyDialogModel
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
                ["Progress"] = () => new ProgressDialog(new Commander.Core.TransferJob("Copying", "/Users/Shared", _ => { })),
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
                ["Update"] = () => new UpdateWindow(UpdateModel(true)),
                ["FileDiff"] = () => new FileDiffWindow(new Commander.ViewModel.FileDiffVM(file.FullName, Path.Combine(AppContext.BaseDirectory, "Commands.xml"), null, logger, null)),
            };
            foreach (var (name, create) in windows)
                ShowAndCapture(create(), name);
        }

        private static UpdateModel UpdateModel(bool canInstall) => new UpdateModel
        {
            Current = "2.1.0", CanInstall = canInstall,
            Release = new ReleaseInfo { Tag = "v2.2.0", Notes = "## What's changed\n- Auto updater (#6)\n- Configurable editor and terminal (#7, #8)" },
        };

        // Each button closes the dialog with its answer (#6).
        [AvaloniaTest]
        public void UpdateWindow_ButtonsAnswer()
        {
            var owner = new Window();
            owner.Show();
            foreach (var (button, expected) in new[] { ("SkipButton", UpdateAnswer.Skip), ("LaterButton", UpdateAnswer.Later), ("DownloadButton", UpdateAnswer.Download) })
            {
                var window = new UpdateWindow(UpdateModel(false));
                var result = window.ShowDialog<UpdateAnswer>(owner);
                Dispatcher.UIThread.RunJobs();
                window.FindControl<Button>(button).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
                Assert.That(result.IsCompleted, Is.True, button);
                Assert.That(result.Result, Is.EqualTo(expected), button);
            }
            Assert.That(new UpdateWindow(UpdateModel(false)).FindControl<Button>("DownloadButton").Content, Is.EqualTo("Open download page"));
            Assert.That(new UpdateWindow(UpdateModel(true)).FindControl<Button>("DownloadButton").Content, Is.EqualTo("Update and restart"));
            owner.Close();
        }

        // T15: Esc answers Later, never Download.
        [AvaloniaTest]
        public void UpdateWindow_Escape_AnswersLater()
        {
            var owner = new Window();
            owner.Show();
            var window = new UpdateWindow(UpdateModel(true));
            var result = window.ShowDialog<UpdateAnswer>(owner);
            Dispatcher.UIThread.RunJobs();
            window.KeyPress(Avalonia.Input.Key.Escape, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();
            Assert.That(result.IsCompleted, Is.True, "Esc closes the dialog");
            Assert.That(result.Result, Is.EqualTo(UpdateAnswer.Later));
            owner.Close();
        }

        private static Commander.Dtos.DeleteDialogModel DeleteModel(bool permanent) => new Commander.Dtos.DeleteDialogModel
        {
            Items = new[] { "image_1.png", "image_2.png", "image.png" }.Select(n => new Commander.Dtos.DeleteItem { Name = n, SizeText = "1.5 MB" }).ToList(),
            Folder = "/Users/me", TotalSizeText = "4.1 MB", Permanent = permanent,
        };
    }
}
