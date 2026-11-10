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
                frame.Save(Path.Combine(dir, name + ".png"));
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
                frame.Save(Path.Combine(dir, "MainWindow.png"));
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
                ["Input"] = () => new InputWindow("Please enter the directory name", "new folder"),
                ["Override"] = () => new OverrideWindow("File a.txt exist in /tmp.\nDo you want to override it ?"),
                ["Zip"] = () => new ZipWindow(Path.GetTempPath()),
                ["UnZip"] = () => new UnZipWindow(file),
                ["Split"] = () => new SplitWindow(logger),
                ["Join"] = () => new JoinWindow(),
                ["History"] = () => new HistoryWindow(new[] { "/tmp", "/usr" }),
                ["Shortcuts"] = () => new ShortcutsWindow(new ShortcutManager()),
                ["Info"] = () => new InfoWindow(file),
                ["Plugin"] = () => new PluginWindow(new NullPluginManager(), "/tmp", new string[0], logger),
                ["Search"] = () => new Search.SearchWindow(null, logger),
            };
            foreach (var (name, create) in windows)
                ShowAndCapture(create(), name);
        }
    }
}
