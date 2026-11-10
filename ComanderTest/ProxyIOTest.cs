using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Dtos;
using IOLib;
using IOLib.Comparators;
using ZipLib;
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
            await Assert.ThatAsync(Copy, Throws.InstanceOf<InvalidOperationException>());

            _panel.OnUnsetBusy = null;
            _dialogs.OverrideAnswer = CopyOption.OverrideSingle;
            await Copy();
            Assert.That(_panel.Errors, Has.None.Contains("in progress"));
            Assert.That(File.ReadAllText(Path.Combine(_target, "a.txt")), Is.EqualTo("new"));
        }

        private IMetadataFileStructure Msg(string path) => (IMetadataFileStructure)_factory.CreateFileMsg(path);

        // BUG-29: a name with a path separator used to move the file elsewhere.
        [TestCase("../escaped.txt")]
        [TestCase("sub/escaped.txt")]
        [TestCase("..")]
        public async Task Rename_ToNameWithPath_IsRefused(string name)
        {
            _dialogs.TextAnswer = name;
            await _proxy.RenameFile(Msg(Path.Combine(_source, "a.txt")));
            Assert.That(File.Exists(Path.Combine(_source, "a.txt")), Is.True);
            Assert.That(_panel.Errors, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Rename_OntoExistingFile_LogsErrorInsteadOfThrowing()
        {
            File.WriteAllText(Path.Combine(_source, "b.txt"), "b");
            _dialogs.TextAnswer = "b.txt";
            await _proxy.RenameFile(Msg(Path.Combine(_source, "a.txt")));
            Assert.That(File.ReadAllText(Path.Combine(_source, "b.txt")), Is.EqualTo("b"));
            Assert.That(_panel.Errors, Has.Count.EqualTo(1));
        }

        // BUG-17: one failing item must not stop the batch, and the result is summarised.
        [Test]
        public async Task Move_BatchWithOneFailure_MovesTheRest_AndLogsSummary()
        {
            File.WriteAllText(Path.Combine(_source, "b.txt"), "b");
            File.WriteAllText(Path.Combine(_source, "c.txt"), "c");
            var files = new[] { "b.txt", "a.txt", "c.txt" }.Select(n => Msg(Path.Combine(_source, n)));

            await _proxy.MoveFile(files, Msg(_target));

            Assert.That(File.Exists(Path.Combine(_target, "b.txt")) && File.Exists(Path.Combine(_target, "c.txt")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(_target, "a.txt")), Is.EqualTo("old"));
            Assert.That(_panel.Errors, Has.Some.Contains("2 moved, 1 failed"));
        }

        // BUG-06 regression: "extract to folder" goes to <panel>/<archive name>.
        [Test]
        public async Task Unzip_ToFolder_UsesArchiveNameUnderCurrentPath()
        {
            var archive = Path.Combine(_root, "my.archive.zip");
            new ZipAdapter().CompressFiles(new ZipParameters(Path.Combine(_root, "my.archive"), ""), Path.Combine(_source, "a.txt"));
            _panel.ActualPath = _target;
            _dialogs.UnzipAnswer = new UnZipRequest { Parameters = new ZipParameters(archive, ""), ExtractType = ExtractType.ToSpecificDir };

            await _proxy.UnZipFiles();

            Assert.That(File.ReadAllText(Path.Combine(_target, "my.archive", "a.txt")), Is.EqualTo("new"));
        }
    }
}
