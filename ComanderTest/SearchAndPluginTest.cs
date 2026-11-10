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
        public void After() => Directory.Delete(_root, true);

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
    }
}
