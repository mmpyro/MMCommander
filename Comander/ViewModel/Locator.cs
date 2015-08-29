using System.IO;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Other;
using IOLib;
using IOLib.Comparators;
using IOLinq;
using LogLib;
using Search;
using Search.ViewModel;

namespace Comander.ViewModel
{
    public class Locator
    {
        private static readonly MainVM _mainVm;
        private static readonly MainWindowEventResolver _mainWindowEventResolver;
        private static readonly GenericCommandManager _genericCommandManager;
        private static readonly SearchVm _searchVm;
        private static readonly SettingsVm _settingsVm;
        

        static Locator()
        {
            _mainWindowEventResolver = new MainWindowEventResolver();
            var logger = new GUILogger(LogLevel.Debug);
            _genericCommandManager = new GenericCommandManager(logger);
            _searchVm = new SearchVm();
            _settingsVm = new SettingsVm();
            Notifier = null;
            IPluginManager pluginManager = CreatePluginManager();

            var shortcutManager = new ShortcutManager();
            var historyManager1 = new HistoryManager();
            var historyManager2 = new HistoryManager();
            var configReader = new ConfigReader(shortcutManager);
            var fileFactory = new MetaDataFileFactory();
            var fileSystemManager = new FileSystemManager(new FileManager( fileFactory), new DriveManager(), new DirectoryManager( fileFactory), new FileNameComparer());
            var syntaxParser = new SyntaxParser(fileFactory);
            var io1 = new IOManager(configReader["IO1"], fileSystemManager,syntaxParser,historyManager1, configReader, _mainWindowEventResolver, pluginManager, logger);
            var io2 = new IOManager(configReader["IO2"], fileSystemManager,syntaxParser,historyManager2, configReader, _mainWindowEventResolver,pluginManager, logger);
            io1.SecondManager = io2;
            io2.SecondManager = io1;
            _mainVm = new MainVM(io1, io2, configReader ,logger);
        }

        public  MainVM Main
        {
            get { return _mainVm; }
        }

        public static GenericCommandManager GenericCommandManager
        {
            get { return _genericCommandManager; }
        }

        public static MainWindowEventResolver MainWindowEventResolver
        {
            get { return _mainWindowEventResolver; }
        }

        public static IFileNotifier Notifier { get; set; }

        public static SearchVm SearchVm
        {
            get { return _searchVm; }
        }

        public static SettingsVm SettingsVm
        {
            get { return _settingsVm; }
        }

        private static IPluginManager CreatePluginManager()
        {
            try
            {
                var plugin = new PluginManager();
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
                plugin.LoadPlugins(path);
                return plugin;
            }
            catch
            {
                return new NullPluginManager();
            }
        }
    }
}