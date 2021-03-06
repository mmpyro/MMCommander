﻿using System.IO;
using Comander.CommanderIO;
using Comander.Core;
using IOLib;
using IOLib.Comparators;
using IOLinq;
using LogLib;
using Search;
using Search.ViewModel;
using System.Windows.Threading;
using System;

namespace Comander.ViewModel
{
    public class Locator
    {
        private static readonly MainVM _mainVm;
        private static readonly SearchVm _searchVm;
        private static readonly SettingsVm _settingsVm;
        private static readonly ILogger _logger;

        static Locator()
        {
            _logger = new ComplexLogger(new GUILogger(), new FileLogger());
            _searchVm = new SearchVm(_logger);
            _settingsVm = new SettingsVm();
            Notifier = null;
            IPluginManager pluginManager = CreatePluginManager();
            IPathResolver pathResolver = new PathResolver();
            var shortcutManager = new ShortcutManager();
            var historyManager1 = new HistoryManager();
            var historyManager2 = new HistoryManager();
            var configReader = new ConfigReader(shortcutManager);
            var fileFactory = new MetaDataFileFactory();
            var fileSystemManager = new FileSystemManager(new FileManager( fileFactory), new DriveManager(), new DirectoryManager( fileFactory), new FileNameComparer());
            var syntaxParser = new SyntaxParser(fileFactory);
            var monitor = new StateMonitor();
            var io1 = new IOManager(ManagerType.Io1, configReader.Io1, fileSystemManager,syntaxParser,historyManager1, configReader, pluginManager, _logger, pathResolver, new ProxyIO(fileSystemManager, monitor));
            var io2 = new IOManager(ManagerType.Io2, configReader.Io2, fileSystemManager,syntaxParser,historyManager2, configReader, pluginManager, _logger, pathResolver, new ProxyIO(fileSystemManager, monitor));
            io1.SecondManager = io2;
            io2.SecondManager = io1;
            _mainVm = new MainVM(io1, io2, configReader , _logger, new GenericCommandManager(io1, io2,_logger), new AssemblyVersionResolver());
            App.Current.Dispatcher.UnhandledException += AppDispatcherUnhandledException;
        }


        public  MainVM Main
        {
            get { return _mainVm; }
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
                var plugin = new PluginManager(_logger);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
                plugin.LoadPlugins(path);
                return plugin;
            }
            catch
            {
                return new NullPluginManager();
            }
        }

        private static void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.Fatal(e.Exception);
            e.Handled = true;
        }
    }
}