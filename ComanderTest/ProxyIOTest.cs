using System;
using System.IO;
using System.Threading.Tasks;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Dtos;
using IOLib;
using IOLib.Comparators;
using NUnit.Framework;

namespace ComanderTest
{
    [TestFixture]
    public class ProxyIOTest
    {
        private string _root, _source, _target;
        private FakeDialogs _dialogs;
        private FakePanel _panel;
        private ProxyIO _proxy;
        private readonly MetaDataFileFactory _factory = new MetaDataFileFactory();

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mmc-proxy-" + Guid.NewGuid().ToString("N"))).FullName;
            _source = Directory.CreateDirectory(Path.Combine(_root, "src")).FullName;
            _target = Directory.CreateDirectory(Path.Combine(_root, "dst")).FullName;
            File.WriteAllText(Path.Combine(_source, "a.txt"), "new");
            File.WriteAllText(Path.Combine(_target, "a.txt"), "old");
            _dialogs = new FakeDialogs();
            _panel = new FakePanel { ActualPath = _source };
            var fileSystem = new FileSystemManager(new FileManager(_factory), new DriveManager(), new DirectoryManager(_factory), new FileNameComparer());
            _proxy = new ProxyIO(fileSystem, new StateMonitor(), _dialogs) { Manager = _panel };
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        private Task Copy() => _proxy.CopyFile(new[] { (IMetadataFileStructure)_factory.CreateFileMsg(Path.Combine(_source, "a.txt")) },
            (IMetadataFileStructure)_factory.GetDirectoryFromPath(_target));

        [Test]
        public async Task Copy_OntoExistingFile_AnsweredDontOverride_KeepsTarget()
        {
            _dialogs.OverrideAnswer = CopyOption.None;
            await Copy();
            Assert.That(_dialogs.OverrideQuestions, Is.EqualTo(1));
            Assert.That(File.ReadAllText(Path.Combine(_target, "a.txt")), Is.EqualTo("old"));
        }

        [Test]
        public async Task Copy_OntoExistingFile_AnsweredOverride_ReplacesTarget()
        {
            _dialogs.OverrideAnswer = CopyOption.OverrideSingle;
            await Copy();
            Assert.That(File.ReadAllText(Path.Combine(_target, "a.txt")), Is.EqualTo("new"));
        }

        [Test]
        public async Task BusyLock_IsReleased_WhenRefreshAfterOperationThrows()
        {
            _panel.OnUnsetBusy = () => throw new InvalidOperationException("refresh failed");
            Assert.ThrowsAsync<InvalidOperationException>(Copy);

            _panel.OnUnsetBusy = null;
            _dialogs.OverrideAnswer = CopyOption.OverrideSingle;
            await Copy();
            Assert.That(_panel.Errors, Has.None.Contains("in progress"));
            Assert.That(File.ReadAllText(Path.Combine(_target, "a.txt")), Is.EqualTo("new"));
        }
    }
}
