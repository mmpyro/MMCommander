using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace Comander.Core
{
    public interface IConfigReader
    {
        ConfigurationProgram this[string key] { get; set; }
        IEnumerable<ConfigurationProgram> GetPrograms();
        IShortcutManager ShortcutManager { get; }
        void SavePaths(string source, string target);
        string Io2 { get; }
        string Io1 { get; }
    }

    public class ConfigReader : IConfigReader
    {
        private readonly string shortCutsPath = Path.Combine(Directory.GetCurrentDirectory(), "Config.xml");
        private readonly RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software", true);
        private IDictionary<string, ConfigurationProgram> _config;
        private readonly IShortcutManager _shortcutManager;
        private Configuration _configuration;

        public ConfigReader(IShortcutManager shortcutManager)
        {
            ReadConfig();
            _shortcutManager = shortcutManager;
            ReadRegisterPaths();
            ReadShortCuts();
            ReadApplications();

        }

        private void ReadApplications()
        {
            _config = (from item in _configuration.AppSection select item).ToDictionary(t => t.Name);
        }

        private void ReadConfig()
        {
            var serializer = new XmlSerializer(typeof (Configuration));
            TextReader textReader = new StreamReader(shortCutsPath);
            _configuration = (Configuration)serializer.Deserialize(textReader);
            textReader.Close();
        }


        private void ReadRegisterPaths()
        {
            var key = registryKey.CreateSubKey("MMCommander");
            string io1Key = (string) (key.GetValue("Source") ?? @"C:\");
            string io2Key = (string) (key.GetValue("Target") ?? @"C:\");
            Io1 = io1Key;
            Io2 = io2Key;
        }

        private void ReadShortCuts()
        {
            foreach (var item in _configuration.ShortCuts)
            {
                _shortcutManager.Add(item);
            }
        }

        private void SavePaths()
        {
            _configuration.ShortCuts = _shortcutManager.GetShortcuts().ToArray();
            var serializer = new XmlSerializer(typeof(Configuration));
            TextWriter textWriter = new StreamWriter(shortCutsPath);
            serializer.Serialize(textWriter, _configuration);
            textWriter.Close();
        }

        public ConfigurationProgram this[string key]
        {
            get { return _config[key]; }
            set { _config[key] = value; }
        }

        public IShortcutManager ShortcutManager
        {
            get { return _shortcutManager; }
        }

        public string Io2 { get; protected set; }
        public string Io1 { get; protected set; }

        public void SavePaths(string source, string target)
        {
            SaveRegisterPaths(source, target);
            SavePaths();
        }

        private void SaveRegisterPaths(string source, string target)
        {
            var key = registryKey.CreateSubKey("MMCommander");
            key.SetValue("Source", source);
            key.SetValue("Target", target);
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
    }

    [XmlType(AnonymousType = true)]
    public partial class ConfigurationProgram
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("workingDirectory")]
        public string WorkingDir { get; set; }

        [XmlText()]
        public string Value { get; set; }
    }
    #endregion

}