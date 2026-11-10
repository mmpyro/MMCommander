using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using ZipLib;

namespace ZipLibTest
{
    [TestFixture]
    public class ArchiveRoundTripTest
    {
        private string _root;
        private string _source;
        private string _target;
        private readonly ZipAdapter _zipAdapter = new ZipAdapter();

        [SetUp]
        public void Before()
        {
            _root = Directory.CreateDirectory(Path.Combine(TestPaths.Root, Guid.NewGuid().ToString("N"))).FullName;
            _source = Directory.CreateDirectory(Path.Combine(_root, "src", "sub")).Parent!.FullName;
            _target = Path.Combine(_root, "out");
            File.WriteAllText(Path.Combine(_source, "a.txt"), "alpha");
            File.WriteAllText(Path.Combine(_source, "sub", "b.txt"), "beta");
        }

        [TearDown]
        public void After() => Directory.Delete(_root, true);

        [TestCase(ZipType.Zip, ".zip")]
        [TestCase(ZipType.Tar, ".tar")]
        [TestCase(ZipType.GZip, ".tar.gz")]
        [TestCase(ZipType.Bzip2, ".tar.bz2")]
        public void CompressDir_ThenExtract_RestoresTree(ZipType type, string extension)
        {
            var name = Path.Combine(_root, "archive");
            _zipAdapter.CompressDir(new ZipParameters(name, "", type), _source);
            _zipAdapter.UnCompressFile(new ZipParameters(name + extension, ""), _target);

            Assert.That(File.ReadAllText(Path.Combine(_target, "a.txt")), Is.EqualTo("alpha"));
            Assert.That(File.ReadAllText(Path.Combine(_target, "sub", "b.txt")), Is.EqualTo("beta"));
        }

        [Test]
        public void EncryptedDir_ExtractsOnlyWithPassword()
        {
            var name = Path.Combine(_root, "secret");
            _zipAdapter.CompressDirWithEncryption(new ZipParameters(name, "!Ma78"), _source);

            Assert.That(() => _zipAdapter.UnCompressFile(new ZipParameters(name + ".zip", "wrong"), _target), Throws.Exception);
            _zipAdapter.UnCompressFile(new ZipParameters(name + ".zip", "!Ma78"), _target);
            Assert.That(File.ReadAllText(Path.Combine(_target, "sub", "b.txt")), Is.EqualTo("beta"));
        }

        [Test]
        public void Password_OnNonZip_IsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                _zipAdapter.CompressDirWithEncryption(new ZipParameters(Path.Combine(_root, "x"), "pw", ZipType.Tar), _source));
        }

        [Test]
        public void EntryOutsideTarget_IsNotWritten()
        {
            var archive = Path.Combine(_root, "slip.zip");
            using (var zip = new ZipOutputStream(File.Create(archive)))
            {
                zip.PutNextEntry(new ZipEntry("../evil.txt"));
                zip.Write("x"u8);
                zip.CloseEntry();
            }

            Assert.Throws<SharpCompress.Common.ExtractionException>(() => _zipAdapter.UnCompressFile(new ZipParameters(archive, ""), _target));
            Assert.That(File.Exists(Path.Combine(_root, "evil.txt")), Is.False);
        }
    }
}
