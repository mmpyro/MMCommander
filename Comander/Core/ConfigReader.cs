using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace Comander.Core
{
    public class ConfigReader
    {
        private readonly string shortCutsPath = Path.Combine(Directory.GetCurrentDirectory(), "Config.xml");
        private readonly RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software", true);
        private readonly IDictionary<string,string> _config = new Dictionary<string, string>();
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
            var tmp = (from item in _configuration.AppSection select item).ToDictionary(t => t.Name, t => t.Value);
            foreach (var item in tmp)
            {
                _config.Add(item);
            }
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
            _config.Add("IO1", io1Key);
            _config.Add("IO2", io2Key);
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

        public string this[string key]
        {
            get { return _config[key]; }
            set { _config[key] = value; }
        }

        public IShortcutManager ShortcutManager
        {
            get { return _shortcutManager; }
        }

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
    }


    #region Configuration
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Configuration
    {
        /// <remarks/>
        [XmlArrayItem("Program", IsNullable = false)]
        public ConfigurationProgram[] AppSection { get; set; }

        /// <remarks/>
        [XmlArrayItem("ShortCut", IsNullable = false)]
        public string[] ShortCuts { get; set; }
    }

    /// <remarks/>
    [XmlType(AnonymousType = true)]
    public partial class ConfigurationProgram
    {
        /// <remarks/>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <remarks/>
        [XmlText()]
        public string Value { get; set; }
    }
    #endregion

}