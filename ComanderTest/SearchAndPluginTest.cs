using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using Comander.Core;
using CommunityToolkit.Mvvm.Input;
using IOLib;
using LogLib;
using NUnit.Framework;
using Search.ViewModel;

namespace ComanderTest
{
    [TestFixture]
    public class SearchAndPluginTest
    {
        private string _root;
        private readonly List<string> _errors = new List<string>();
        private GUILogger _logger;

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-search-" + Guid.NewGuid().ToString("N"))).FullName;
            _logger = new GUILogger();
            _logger.NotifyEvent += (info, message) => { if (info == LogInfo.Error) _errors.Add(message); };
            _errors.Clear();
        }

        [TearDown]
        public void After()
        {
            try { Directory.Delete(_root, true); }
            catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows()) { } // loaded plugin DLLs stay locked until the process exits
        }

        // BUG-14 regression: after a search the Search button must be usable again.
        [AvaloniaTest]
        public async Task Search_Finishes_AndCanRunAgain()
        {
            File.WriteAllText(Path.Combine(_root, "hit.txt"), "");
            var vm = new SearchVm(_logger, null, () => { })
            {
                Parameters = new SearchParameters(_root, "hit", true) { MatchOptions = MatchOptions.Contains }
            };
            var start = (IAsyncRelayCommand)vm.StartCommand;

            await start.ExecuteAsync(null);
            Assert.That(vm.IsRunning, Is.False);
            await start.ExecuteAsync(null);
            Assert.That(vm.IsRunning, Is.False);
            Assert.That(_errors, Is.Empty);
        }

        // BUG-20 regression: junk in Plugins/ is logged and skipped, it must not disable plugin loading.
        [Test]
        public void LoadPlugins_SkipsJunkFiles()
        {
            File.WriteAllText(Path.Combine(_root, "README.txt"), "not a plugin");
            File.WriteAllText(Path.Combine(_root, "broken.dll"), "not an assembly");
            var manager = new PluginManager(_logger);

            Assert.DoesNotThrow(() => manager.LoadPlugins(_root));
            Assert.That(manager.GetMethods(), Is.Empty);
            Assert.That(_errors, Has.Count.EqualTo(1).And.Some.Contains("broken.dll"));
        }

        // A plugin in its own folder loads in its own context, even when it ships its own CommanderPlugin.dll copy.
        [Test]
        public async Task LoadPlugins_FolderPlugin_ListedAndRuns()
        {
            var folder = Directory.CreateDirectory(Path.Combine(_root, "Sample")).FullName;
            File.Copy(typeof(TouchPlugin).Assembly.Location, Path.Combine(folder, "Sample.dll"));
            File.Copy(typeof(CommanderPlugin.ICommanderPlugin).Assembly.Location, Path.Combine(folder, "CommanderPlugin.dll"));
            var manager = new PluginManager(_logger);

            manager.LoadPlugins(_root);
            Assert.That(manager.GetMethods(), Is.EqualTo(new[] { "Touch" }));
            await manager.InvokeMethod("Touch", _root, new string[0]);

            Assert.That(File.Exists(Path.Combine(_root, "touched.txt")), Is.True);
            Assert.That(_errors, Is.Empty);
        }

        // The sample plugin, installed the way docs/PLUGINS.md says: Plugins/Sha256Sums/Sha256Sums.dll.
        private PluginManager InstallSha256Sums()
        {
            var folder = Directory.CreateDirectory(Path.Combine(_root, "Plugins", "Sha256Sums")).FullName;
            File.Copy(typeof(Sha256Sums.Sha256Sums).Assembly.Location, Path.Combine(folder, "Sha256Sums.dll"));
            var manager = new PluginManager(_logger);
            manager.LoadPlugins(Path.Combine(_root, "Plugins"));
            return manager;
        }

        [Test]
        public async Task Sha256Sums_WritesSumsFile()
        {
            var file = Path.Combine(_root, "abc.txt");
            File.WriteAllText(file, "abc");
            var manager = InstallSha256Sums();

            await manager.InvokeMethod("SHA-256 sums", _root, new[] { file });

            // NIST FIPS 180-2 test vector for "abc".
            Assert.That(File.ReadAllText(Path.Combine(_root, "SHA256SUMS")).TrimEnd(),
                Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad  abc.txt"));
            Assert.That(_errors, Is.Empty);
        }

        [Test]
        public async Task Sha256Sums_NoFilesSelected_LogsError()
        {
            var manager = InstallSha256Sums();

            await manager.InvokeMethod("SHA-256 sums", _root, new[] { Path.Combine(_root, "Plugins") });

            Assert.That(File.Exists(Path.Combine(_root, "SHA256SUMS")), Is.False);
            Assert.That(_errors, Has.Count.EqualTo(1).And.Some.EndsWith("Select one or more files"));
        }
    }

    // Loaded by LoadPlugins_FolderPlugin_ListedAndRuns from a copy of this assembly.
    public class TouchPlugin : CommanderPlugin.ICommanderPlugin
    {
        public string Name => "Touch";

        public Task<CommanderPlugin.Response> InvokeAsync(string dir, IEnumerable<string> files)
        {
            File.WriteAllText(Path.Combine(dir, "touched.txt"), "");
            return Task.FromResult(new CommanderPlugin.Response(CommanderPlugin.OperationStatus.Success));
        }
    }
}
