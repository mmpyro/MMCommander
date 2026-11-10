using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Comander.Core
{
    public interface IConfigReader
    {
        ConfigurationProgram this[string key] { get; set; }
        IEnumerable<ConfigurationProgram> GetPrograms();
        IShortcutManager ShortcutManager { get; }
        void SavePaths(string source, string target, string[] sourceTabs = null, string[] targetTabs = null);
        string Io2 { get; }
        string Io1 { get; }
        // Extra folder tabs per panel; empty for configs written before tabs existed.
        string[] Io1Tabs { get; }
        string[] Io2Tabs { get; }
        // User key bindings: command id -> "F5;C" (Core/CommandRegistry.cs). Empty means all defaults.
        IDictionary<string, string> KeyBindings { get; }
        // Small app settings (appearance, confirmations); null when not set.
        string GetSetting(string key);
        void SaveSetting(string key, string value);
        void SaveKeyBindings(IDictionary<string, string> bindings);
    }

    public class ConfigReader : IConfigReader
    {
        public static readonly string DefaultConfigPath = Path.Combine(AppContext.BaseDirectory, "Config.xml");
        public static readonly string UserConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MMCommander", "Config.xml");

        private readonly string _defaultPath;
        private readonly string _userPath;
        private IDictionary<string, ConfigurationProgram> _config;
        private Configuration _configuration;

        public ConfigReader(IShortcutManager shortcutManager) : this(shortcutManager, DefaultConfigPath, UserConfigPath)
        {
        }

        // Read-only defaults ship next to the binary; the user's copy (shortcuts, last paths) lives in app data.
        public ConfigReader(IShortcutManager shortcutManager, string defaultPath, string userPath)
        {
            _defaultPath = defaultPath;
            _userPath = userPath;
            ReadConfig();
            ShortcutManager = shortcutManager;
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Io1 = Directory.Exists(_configuration.Source) ? _configuration.Source : home;
            Io2 = Directory.Exists(_configuration.Target) ? _configuration.Target : home;
            Io1Tabs = _configuration.SourceTabs ?? new string[0];
            Io2Tabs = _configuration.TargetTabs ?? new string[0];
            KeyBindings = (_configuration.KeyBindings ?? new ConfigurationKeyBinding[0])
                .Where(k => !string.IsNullOrEmpty(k.Command)).GroupBy(k => k.Command).ToDictionary(g => g.Key, g => g.Last().Keys ?? string.Empty);
            ReadShortCuts();
            ReadApplications();
        }

        private void ReadApplications()
        {
            _config = (from item in _configuration.AppSection ?? new ConfigurationProgram[0]
                       where string.IsNullOrEmpty(item.Os) || OperatingSystem.IsOSPlatform(item.Os)
                       select item).ToDictionary(t => t.Name);
        }

        private void ReadConfig()
        {
            var serializer = new XmlSerializer(typeof (Configuration));
            using var textReader = new StreamReader(File.Exists(_userPath) ? _userPath : _defaultPath);
            _configuration = (Configuration)serializer.Deserialize(textReader);
        }

        private void ReadShortCuts()
        {
            foreach (var item in _configuration.ShortCuts ?? new string[0])
            {
                ShortcutManager.Add(item);
            }
        }

        // Write to a temp file first so a failed save never leaves a truncated config (BUG-18).
        private void SaveConfig()
        {
            _configuration.ShortCuts = ShortcutManager.GetShortcuts().ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(_userPath));
            var tempPath = _userPath + ".tmp";
            var serializer = new XmlSerializer(typeof(Configuration));
            using (var textWriter = new StreamWriter(tempPath))
                serializer.Serialize(textWriter, _configuration);
            File.Move(tempPath, _userPath, true);
        }

        public ConfigurationProgram this[string key]
        {
            get { return _config[key]; }
            set { _config[key] = value; }
        }

        public IShortcutManager ShortcutManager { get; }

        public string Io2 { get; protected set; }
        public string Io1 { get; protected set; }
        public string[] Io1Tabs { get; protected set; }
        public string[] Io2Tabs { get; protected set; }
        public IDictionary<string, string> KeyBindings { get; protected set; }

        public void SavePaths(string source, string target, string[] sourceTabs = null, string[] targetTabs = null)
        {
            _configuration.Source = source;
            _configuration.Target = target;
            _configuration.SourceTabs = sourceTabs;
            _configuration.TargetTabs = targetTabs;
            SaveConfig();
        }

        public string GetSetting(string key) =>
            _configuration.Settings?.FirstOrDefault(s => s.Key == key)?.Value;

        public void SaveSetting(string key, string value)
        {
            var list = (_configuration.Settings ?? new ConfigurationSetting[0]).Where(s => s.Key != key).ToList();
            if (value != null)
                list.Add(new ConfigurationSetting { Key = key, Value = value });
            _configuration.Settings = list.ToArray();
            SaveConfig();
        }

        public void SaveKeyBindings(IDictionary<string, string> bindings)
        {
            KeyBindings = new Dictionary<string, string>(bindings);
            _configuration.KeyBindings = bindings.Select(b => new ConfigurationKeyBinding { Command = b.Key, Keys = b.Value }).ToArray();
            SaveConfig();
        }

        public IEnumerable<ConfigurationProgram> GetPrograms()
        {
            return _config.Select(t => t.Value);
        }
    }


    #region Configuration
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Configuration
    {
        [XmlArrayItem("Program", IsNullable = false)]
        public ConfigurationProgram[] AppSection { get; set; }

        [XmlArrayItem("ShortCut", IsNullable = false)]
        public string[] ShortCuts { get; set; }

        public string Source { get; set; }
        public string Target { get; set; }

        [XmlArrayItem("Tab", IsNullable = false)]
        public string[] SourceTabs { get; set; }

        [XmlArrayItem("Tab", IsNullable = false)]
        public string[] TargetTabs { get; set; }

        [XmlArrayItem("Binding", IsNullable = false)]
        public ConfigurationKeyBinding[] KeyBindings { get; set; }

        [XmlArrayItem("Setting", IsNullable = false)]
        public ConfigurationSetting[] Settings { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public partial class ConfigurationSetting
    {
        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public partial class ConfigurationKeyBinding
    {
        [XmlAttribute("command")]
        public string Command { get; set; }

        // Avalonia gestures separated by ';', e.g. "F5;C". Empty unbinds the command.
        [XmlAttribute("keys")]
        public string Keys { get; set; }
    }

    [XmlType(AnonymousType = true)]
    public partial class ConfigurationProgram
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("workingDirectory")]
        public string WorkingDir { get; set; }

        // "windows", "osx" or "linux"; empty means every OS.
        [XmlAttribute("os")]
        public string Os { get; set; }

        // Arguments; each "{path}" token is replaced by the file or folder the program is run for.
        [XmlAttribute("args")]
        public string Args { get; set; }

        [XmlText()]
        public string Value { get; set; }
    }
    #endregion

}