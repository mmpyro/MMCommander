using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Comander;
using Comander.CommanderIO;
using Comander.Core;
using Comander.View;
using LogLib;
using NUnit.Framework;
using MainVM = Comander.ViewModel.MainVM;

namespace ComanderTest
{
    // Renders the README images from a demo folder and a throwaway config, never the user's own files or settings.
    // Run through docs/make-screenshots.sh (sets MMC_DOCS). Never closes MainWindow and never presses Settings > Done.
    [TestFixture, Explicit("writes the README images; run docs/make-screenshots.sh")]
    public class DocsScreenshots
    {
        private static readonly string Root = OperatingSystem.IsWindows()
            ? Path.Combine(Path.GetTempPath(), "mmc-demo") : "/tmp/mmc-demo";
        private string Site => Path.Combine(Root, "Projects", "aurora-site");
        private string Backup => Path.Combine(Root, "Backup");

        private string _out;
        private int _frame;
        private Window _window;
        private MainVM _vm;

        [SetUp]
        public void Before()
        {
            _out = Environment.GetEnvironmentVariable("MMC_DOCS");
            if (string.IsNullOrEmpty(_out)) Assert.Ignore("set MMC_DOCS=<dir>");
            Directory.CreateDirectory(Path.Combine(_out, "frames"));
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
            CreateDemoTree();
        }

        [TearDown]
        public void After()
        {
            foreach (var owned in _window?.OwnedWindows.ToList() ?? new System.Collections.Generic.List<Window>())
                owned.Close();
            Pump(5);
            AppSettings.Current.Theme = ThemeChoice.Dark;
            AppSettings.Current.Accent = AppSettings.Accents[0];
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        [AvaloniaTest]
        public void Render()
        {
            _vm = App.CreateMainVm(new ComplexLogger(new GUILogger()), Path.Combine(Root, "Config.xml"));
            _window = new MainWindow { DataContext = _vm, Width = 1280, Height = 760 };
            _window.Show();
            PumpUntil(() => RowNames(1).Contains("photos") && RowNames(2).Contains("old-notes.md"));
            ClickRow(1, "assets");
            Frame();

            // Marking: Space toggles the cursor row, Down moves on.
            ClickRow(1, "README.md");
            Frame();
            for (int i = 0; i < 3; i++) { Key(Avalonia.Input.Key.Space); Key(Avalonia.Input.Key.Down); Frame(); }
            Key(Avalonia.Input.Key.Up);
            Save(Capture(), "main");

            // F5: options dialog, then Enter copies to the right panel.
            Key(Avalonia.Input.Key.F5);
            var copy = Dialog<CopyDialog>();
            var copyShot = Compose(Capture(), copy);
            Save(copyShot, "copy");
            FrameOf(copyShot);
            copy.KeyPress(Avalonia.Input.Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            PumpUntil(() => RowNames(2).Contains("README.md"), 10000);
            CloseOwned();
            Frame();

            // Command palette.
            Key(Avalonia.Input.Key.K, RawInputModifiers.Control);
            Frame();
            Type("zi");
            Save(Capture(), "palette");
            Frame();
            Key(Avalonia.Input.Key.Escape);

            // Filter language.
            _vm.Io1.Filter = "ext -> (png, pdf)";
            PumpUntil(() => !RowNames(1).Contains("README.md"), 4000);
            Save(Capture(), "filter");
            Frame();
            _vm.Io1.Filter = "";
            PumpUntil(() => RowNames(1).Contains("README.md"), 4000);

            // Thumbnails of the photos folder.
            _vm.Io1.ActualPath = Path.Combine(Site, "photos");
            PumpUntil(() => _vm.Io1.Files.Any(f => f.Name == "sunrise.png"));
            _vm.Io1.ViewIndex = 1;
            Pump(40);
            Save(Capture(), "thumbnails");
            Frame();
            _vm.Io1.ViewIndex = 0;
            _vm.Io1.ActualPath = Site;
            PumpUntil(() => RowNames(1).Contains("photos"));

            // Compare directories.
            _vm.CompareDirCommand.Execute(null);
            var compare = Dialog<CompareDirsWindow>();
            Pump(40);
            Save(Compose(Capture(), compare), "compare");
            CloseOwned();

            // Settings > Appearance; closed without Done, so nothing is saved.
            _vm.SettingsCommand.Execute(null);
            var settings = Dialog<SettingsWindow>();
            Pump(10);
            Save(Compose(Capture(), settings), "settings");
            CloseOwned();

            // Black theme with another accent, in memory only (reset in After).
            AppSettings.Current.Theme = ThemeChoice.Black;
            AppSettings.Current.Accent = AppSettings.Accents[2];
            Pump(10);
            Save(Capture(), "black-theme");

            Assert.That(File.Exists(Path.Combine(_out, "main.png")), Is.True);
        }

        // ---------- demo data ----------

        private void CreateDemoTree()
        {
            var now = new DateTime(2026, 11, 10, 14, 30, 0);
            foreach (var d in new[] { "src", "docs", "assets", "photos" })
                Directory.CreateDirectory(Path.Combine(Site, d));
            Directory.CreateDirectory(Backup);
            File.WriteAllText(Path.Combine(Site, "src", "index.ts"), "export const hello = 'world';\n");
            File.WriteAllText(Path.Combine(Site, "src", "app.css"), "body { margin: 0; }\n");
            Bytes(Path.Combine(Site, "src", "bundle.js"), 640_000, now.AddDays(-2));
            Bytes(Path.Combine(Site, "README.md"), 4_200, now.AddDays(-1));
            Bytes(Path.Combine(Site, "package.json"), 1_350, now.AddDays(-3));
            Bytes(Path.Combine(Site, "build.sh"), 820, now.AddDays(-12));
            Bytes(Path.Combine(Site, "report-2026.pdf"), 2_480_000, now.AddDays(-6));
            Bytes(Path.Combine(Site, "budget.xlsx"), 96_000, now.AddDays(-20));
            Bytes(Path.Combine(Site, "notes.txt"), 2_100, now.AddHours(-5));
            Bytes(Path.Combine(Site, "logo.png"), 48_000, now.AddDays(-40));
            ZipFile.CreateFromDirectory(Path.Combine(Site, "src"), Path.Combine(Site, "release.zip"));
            File.SetLastWriteTime(Path.Combine(Site, "release.zip"), now.AddDays(-2));
            var photos = new[] { ("sunrise", "#FF9A5B", "#7B3FA0"), ("forest", "#2E8B57", "#0B3D2E"), ("ocean", "#1E90FF", "#0A2A66"),
                                 ("desert", "#F4C27A", "#B5651D"), ("city", "#5B6CFF", "#1A1A3A"), ("aurora", "#3CC6B0", "#2A1A5E") };
            foreach (var (name, from, to) in photos)
                Gradient(Path.Combine(Site, "photos", name + ".png"), Color.Parse(from), Color.Parse(to));
            Bytes(Path.Combine(Backup, "old-notes.md"), 3_100, now.AddDays(-90));
            Bytes(Path.Combine(Backup, "package.json"), 1_200, now.AddDays(-60));
            Bytes(Path.Combine(Backup, "report-2025.pdf"), 1_900_000, now.AddDays(-300));
            foreach (var d in Directory.GetDirectories(Site).Concat(new[] { Site, Backup }))
                Directory.SetLastWriteTime(d, now.AddDays(-d.Length % 9));
            File.WriteAllText(Path.Combine(Root, "Config.xml"), $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Configuration>
  <AppSection>
    <Program name=""notepad"" os=""osx"" args=""-t {{path}}"">open</Program>
  </AppSection>
  <ShortCuts><ShortCut>{Site}</ShortCut><ShortCut>{Backup}</ShortCut></ShortCuts>
  <Source>{Site}</Source>
  <Target>{Backup}</Target>
  <SourceTabs><Tab>{Path.Combine(Site, "src")}</Tab></SourceTabs>
  <TargetTabs><Tab>{Path.Combine(Root, "Projects")}</Tab></TargetTabs>
</Configuration>");
        }

        private static void Bytes(string path, int size, DateTime time)
        {
            var data = new byte[size];
            new Random(size).NextBytes(data);
            File.WriteAllBytes(path, data);
            File.SetLastWriteTime(path, time);
        }

        private static void Gradient(string path, Color from, Color to)
        {
            using var bmp = new RenderTargetBitmap(new PixelSize(480, 320));
            using (var ctx = bmp.CreateDrawingContext())
            {
                var brush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = { new GradientStop(from, 0), new GradientStop(to, 1) },
                };
                ctx.FillRectangle(brush, new Rect(0, 0, 480, 320));
                ctx.DrawEllipse(new SolidColorBrush(Colors.White, 0.35), null, new Point(340, 110), 46, 46);
            }
            bmp.Save(path, PngBitmapEncoderOptions.Default);
        }

        // ---------- capture ----------

        private Bitmap Capture() => _window.CaptureRenderedFrame();

        // The app frame, dimmed, with the dialog's own frame centred on it (dialogs are separate headless windows).
        private Bitmap Compose(Bitmap main, Window dialog)
        {
            Pump(5);
            var top = dialog.CaptureRenderedFrame();
            var size = main.PixelSize;
            var rtb = new RenderTargetBitmap(size);
            using (var ctx = rtb.CreateDrawingContext())
            {
                var full = new Rect(0, 0, size.Width, size.Height);
                ctx.DrawImage(main, full);
                ctx.FillRectangle(new SolidColorBrush(Colors.Black, 0.45), full);
                var w = Math.Min(top.PixelSize.Width, size.Width);
                var h = Math.Min(top.PixelSize.Height, size.Height);
                var dest = new Rect((size.Width - w) / 2.0, (size.Height - h) / 2.0, w, h);
                // Clip to the card's radius (Radius.Dialog): headless paints the transparent corners white.
                using (ctx.PushClip(new RoundedRect(dest, 14)))
                    ctx.DrawImage(top, new Rect(0, 0, w, h), dest);
            }
            return rtb;
        }

        private void Save(Bitmap bitmap, string name) => bitmap.Save(Path.Combine(_out, name + ".png"), PngBitmapEncoderOptions.Default);

        // Every GIF frame is redrawn into a RenderTargetBitmap so all PNGs share one pixel format (RGBA);
        // a format change mid-sequence makes ffmpeg's paletteuse drop the remaining frames.
        private void FrameOf(Bitmap bitmap)
        {
            using var rtb = new RenderTargetBitmap(bitmap.PixelSize);
            using (var ctx = rtb.CreateDrawingContext())
                ctx.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
            rtb.Save(Path.Combine(_out, "frames", $"{_frame++:000}.png"), PngBitmapEncoderOptions.Default);
        }

        private void Frame() => FrameOf(Capture());

        // ---------- input (same pattern as UiFlowTest) ----------

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

        private DataGrid Grid(int panel) => _window.GetVisualDescendants().OfType<DataGrid>().ElementAt(panel - 1);

        private string[] RowNames(int panel) => Grid(panel).GetVisualDescendants().OfType<DataGridRow>()
            .Where(r => r.IsVisible).Select(r => (r.DataContext as IMetadataFileStructure)?.Name).Where(n => n != null).ToArray();

        private void ClickRow(int panel, string name)
        {
            var row = Grid(panel).GetVisualDescendants().OfType<DataGridRow>().First(r => (r.DataContext as IMetadataFileStructure)?.Name == name);
            var p = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), _window).Value;
            _window.MouseDown(p, MouseButton.Left);
            _window.MouseUp(p, MouseButton.Left);
            Pump();
        }

        private void Key(Key key, RawInputModifiers mods = RawInputModifiers.None)
        {
            _window.KeyPress(key, mods, PhysicalKey.None, null);
            _window.KeyRelease(key, mods, PhysicalKey.None, null);
            Pump();
        }

        private void Type(string text)
        {
            foreach (var c in text) { _window.KeyTextInput(c.ToString()); Pump(3); }
            Pump();
        }

        private T Dialog<T>() where T : Window
        {
            PumpUntil(() => _window.OwnedWindows.OfType<T>().Any());
            return _window.OwnedWindows.OfType<T>().Last();
        }

        private void CloseOwned()
        {
            foreach (var owned in _window.OwnedWindows.ToList()) owned.Close();
            Pump();
        }
    }
}
