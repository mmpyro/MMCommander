using System;
using System.IO;
using Comander.Core;
using NUnit.Framework;

namespace ComanderTest
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
    }
}
