using System;
using System.IO;
using System.Text.RegularExpressions;
using Commander.Core;
using NUnit.Framework;

namespace CommanderTest
{
    [TestFixture]
    public class ConfigReaderTest
    {
        private string _root, _defaultPath, _userPath;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-config-" + Guid.NewGuid().ToString("N"))).FullName;
            _defaultPath = Path.Combine(_root, "Config.xml");
            _userPath = Path.Combine(_root, "user", "Config.xml");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Config.xml"), _defaultPath);
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        [Test]
        public void WithoutUserConfig_ReadsDefaults_AndStartsInHome()
        {
            var reader = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Assert.That(reader.Io1, Is.EqualTo(home));
            // Only the current OS's entries are loaded.
            Assert.That(reader["notepad"].Value, Is.Not.Empty);
            Assert.That(reader.GetPrograms(), Has.Exactly(2).Items);
        }

        [Test]
        public void SavePaths_WritesUserConfig_AndLeavesDefaultsUntouched()
        {
            var before = File.ReadAllText(_defaultPath);
            var shortcuts = new ShortcutManager();
            var reader = new ConfigReader(shortcuts, _defaultPath, _userPath);
            shortcuts.Add(_root);
            reader.SavePaths(_root, Path.GetTempPath());

            var reread = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            Assert.That(reread.Io1, Is.EqualTo(_root));
            Assert.That(reread.ShortcutManager.GetShortcuts(), Does.Contain(_root));
            Assert.That(File.ReadAllText(_defaultPath), Is.EqualTo(before));
            Assert.That(File.Exists(_userPath + ".tmp"), Is.False);
        }
    
        [Test]
        public void SavePaths_KeepsTabs_AndOldConfigsHaveNone()
        {
            var reader = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            Assert.That(reader.Io1Tabs, Is.Empty, "a config written before tabs existed loads with no extra tabs");
            reader.SavePaths(_root, _root, new[] { _root, Path.GetTempPath() }, null);

            var reread = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            Assert.That(reread.Io1Tabs, Is.EqualTo(new[] { _root, Path.GetTempPath() }));
            Assert.That(reread.Io2Tabs, Is.Empty);
        }
    
        [Test]
        public void AppSettings_RoundTripThroughUserConfig()
        {
            var reader = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            var settings = new AppSettings { Theme = ThemeChoice.Black, Accent = "#3CC6B0", Comfortable = true, SeparateExt = false, DefaultPolicy = IOLib.ConflictPolicy.KeepBoth,
                EditorCommand = "\"C:\\Program Files\\x.exe\" {path}", TerminalCommand = "wt -d {path}", CheckForUpdates = false };
            settings.Save(reader);
            reader.SaveKeyBindings(new System.Collections.Generic.Dictionary<string, string> { ["copy"] = "F5" });

            var reread = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            var loaded = AppSettings.Load(reread);
            Assert.That(loaded.Theme, Is.EqualTo(ThemeChoice.Black));
            Assert.That(loaded.Accent, Is.EqualTo("#3CC6B0"));
            Assert.That(loaded.RowHeight, Is.EqualTo(34));
            Assert.That(loaded.SeparateExt, Is.False);
            Assert.That(loaded.DefaultPolicy, Is.EqualTo(IOLib.ConflictPolicy.KeepBoth));
            Assert.That(loaded.EditorCommand, Is.EqualTo("\"C:\\Program Files\\x.exe\" {path}"));
            Assert.That(loaded.TerminalCommand, Is.EqualTo("wt -d {path}"));
            Assert.That(loaded.CheckForUpdates, Is.False);
            Assert.That(reread.KeyBindings["copy"], Is.EqualTo("F5"));
            AppSettings.Load(null);
        }

        // The user file used to keep its own copy of the programs, so editing the shipped Config.xml did nothing.
        [Test]
        public void UserConfig_DoesNotFreezePrograms()
        {
            new ConfigReader(new ShortcutManager(), _defaultPath, _userPath).SavePaths(_root, _root);
            File.WriteAllText(_defaultPath, Regex.Replace(File.ReadAllText(_defaultPath), "(<Program name=\"notepad\"[^>]*>)[^<]*", "$1new-editor"));

            var reread = new ConfigReader(new ShortcutManager(), _defaultPath, _userPath);
            Assert.That(reread["notepad"].Value, Is.EqualTo("new-editor"));
        }
    }
}
