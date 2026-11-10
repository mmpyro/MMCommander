using System;
using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Comander.CommanderIO;
using Comander.Converters;
using NUnit.Framework;

namespace ComanderTest
{
    [TestFixture]
    public class ConverterTest
    {
        [TestCase(0L, "0 KB")]
        [TestCase(8_000L, "8 KB")]
        [TestCase(22_528L, "22 KB")]
        [TestCase(1_048_575L, "1024 KB")]
        [TestCase(3_145_728L, "3.0 MB")]
        [TestCase(1_610_612_736L, "1.5 GB")]
        public void Size_UsesAutoUnits(long bytes, string expected)
        {
            Assert.That(new FileSizeConverter().Convert(bytes, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo(expected));
        }

        [Test]
        public void Size_ShowsDashForFolders()
        {
            var dir = new DirMetaDataStructure(Path.GetTempPath(), null);
            Assert.That(new FileSizeConverter().Convert(dir, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo("—"));
        }

        [Test]
        public void Date_IsIsoMinutes_AndEmptyForMinValue()
        {
            var c = new DateConverter();
            Assert.That(c.Convert(new DateTime(2026, 10, 20, 16, 57, 31), typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo("2026-10-20 16:57"));
            Assert.That(c.Convert(DateTime.MinValue, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo(string.Empty));
        }

        [TestCase(true, "", "Brush.File.Folder")]
        [TestCase(false, ".PNG", "Brush.File.Image")]
        [TestCase(false, ".gz", "Brush.File.Archive")]
        [TestCase(false, ".yaml", "Brush.File.Config")]
        [TestCase(false, ".sql", "Brush.File.Data")]
        [TestCase(false, ".txt", "Brush.File.Other")]
        [TestCase(false, null, "Brush.File.Other")]
        public void FileKind_PicksBrushByExtension(bool isDir, string ext, string expected)
        {
            Assert.That(FileKindBrushConverter.KeyFor(isDir, ext), Is.EqualTo(expected));
        }

        // Guards the guard: a binding typo must reach BindingErrorSink (and would fail any UI test).
        [AvaloniaTest]
        public void BindingTypo_IsCaughtBySink()
        {
            var window = new Window { Content = new TextBlock { [!TextBlock.TextProperty] = new Binding("NoSuchProperty") }, DataContext = new object() };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.That(BindingErrorSink.Errors, Is.Not.Empty);
            BindingErrorSink.Errors.Clear();
            window.Close();
        }
    }
}
