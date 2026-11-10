using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Comander.CommanderIO;
using Comander.Core;
using Comander.View;
using Comander.ViewModel;
using IOLib;
using IOLib.Comparators;
using IOLinq;
using LogLib;
using System.IO;

namespace Comander
{
    public partial class App : Application
    {
        private ILogger _logger;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Composition root (was the Locator static constructor). Any failure shows a window instead of crashing (BUG-08).
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                base.OnFrameworkInitializationCompleted();
                return;
            }
            try
            {
                _logger = new ComplexLogger(new GUILogger(), new FileLogger());
                Dispatcher.UIThread.UnhandledException += (_, e) =>
                {
                    _logger.Fatal(e.Exception);
                    e.Handled = true;
                };
                desktop.MainWindow = new MainWindow { DataContext = CreateMainVm(_logger) };
            }
            catch (Exception e)
            {
                desktop.MainWindow = new Window
                {
                    Title = "MMCommander failed to start",
                    Width = 700,
                    Height = 300,
                    Content = new TextBox { Text = e.ToString(), IsReadOnly = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                };
            }
            base.OnFrameworkInitializationCompleted();
        }

        public static MainVM CreateMainVm(ILogger logger)
        {
            IPluginManager pluginManager = CreatePluginManager(logger);
            IPathResolver pathResolver = new PathResolver();
            var shortcutManager = new ShortcutManager();
            var configReader = new ConfigReader(shortcutManager);
            var settings = AppSettings.Load(configReader);
            settings.Apply(Current);
            // Settings > Appearance applies live: every change rewrites the token brushes.
            settings.PropertyChanged += (_, _) => settings.Apply(Current);
            var fileFactory = new MetaDataFileFactory();
            var fileSystemManager = new FileSystemManager(new FileManager(fileFactory), new DriveManager(), new DirectoryManager(fileFactory), new FileNameComparer());
            var syntaxParser = new SyntaxParser(fileFactory);
            var monitor = new StateMonitor();
            var dialogs = new DialogService(configReader);
            var jobs = new JobQueue(monitor);
            var io1 = new IOManager(ManagerType.Io1, configReader.Io1, fileSystemManager, syntaxParser, new HistoryManager(), configReader, pluginManager, logger, pathResolver, new ProxyIO(fileSystemManager, monitor, dialogs, jobs), dialogs);
            var io2 = new IOManager(ManagerType.Io2, configReader.Io2, fileSystemManager, syntaxParser, new HistoryManager(), configReader, pluginManager, logger, pathResolver, new ProxyIO(fileSystemManager, monitor, dialogs, jobs), dialogs);
            io1.SecondManager = io2;
            io2.SecondManager = io1;
            io1.HideHidden = io2.HideHidden = !settings.ShowHidden;
            io1.RestoreTabs(configReader.Io1Tabs);
            io2.RestoreTabs(configReader.Io2Tabs);
            return new MainVM(io1, io2, configReader, logger, new GenericCommandManager(io1, io2, logger), new AssemblyVersionResolver(), jobs, dialogs);
        }

        private static IPluginManager CreatePluginManager(ILogger logger)
        {
            try
            {
                var plugin = new PluginManager(logger);
                plugin.LoadPlugins(Path.Combine(AppContext.BaseDirectory, "Plugins"));
                return plugin;
            }
            catch (Exception e)
            {
                logger.Error("Plugins disabled", e);
                return new NullPluginManager();
            }
        }
    }
}
