using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace ZipLib
{
    public class ZipAdapter
    {
        public void CompressFiles(ZipParameters zipParameters, params string[] filePaths)
        {
            Write(zipParameters, null, filePaths.SelectMany(Entries).ToList());
        }

        public void CompressFilesWithEncryption(ZipParameters zipParameters, params string[] filePaths)
        {
            Write(zipParameters, zipParameters.Password, filePaths.SelectMany(Entries).ToList());
        }

        // Stores the directory's contents (not the directory itself) at the archive root.
        public void CompressDir(ZipParameters zipParameters, string dir)
        {
            Write(zipParameters, null, DirEntries(dir, "").ToList());
        }

        public void CompressDirWithEncryption(ZipParameters zipParameters, string dir)
        {
            Write(zipParameters, zipParameters.Password, DirEntries(dir, "").ToList());
        }

        /// <summary>
        /// Works as extract all
        /// </summary>
        public void UnCompressFile(ZipParameters zipParameters, string dir)
        {
            var readerOptions = new ReaderOptions { Password = string.IsNullOrEmpty(zipParameters.Password) ? null : zipParameters.Password };
            // ExtractFullPath keeps folders; SharpCompress rejects entries that resolve outside dir (zip-slip).
            var options = new ExtractionOptions { ExtractFullPath = true, Overwrite = true };
            Directory.CreateDirectory(dir);
            if (ArchiveFactory.IsArchive(zipParameters.ArchiveName, out var type) && type == ArchiveType.SevenZip)
            {
                // 7z needs random access, so it can't go through the streaming reader.
                using var archiveStream = File.OpenRead(zipParameters.ArchiveName);
                using var archive = ArchiveFactory.OpenArchive(archiveStream, readerOptions);
                archive.WriteToDirectory(dir, options);
                return;
            }
            // Our own stream, so a failed extraction (wrong password) can't leave the archive locked on Windows.
            using var stream = File.OpenRead(zipParameters.ArchiveName);
            // The reader also unpacks the tar inside .tar.gz / .tar.bz2.
            using var reader = ReaderFactory.OpenReader(stream, readerOptions);
            reader.WriteAllToDirectory(dir, options);
        }

        public static string ArchivePath(ZipParameters zipParameters)
        {
            switch (zipParameters.Type)
            {
                case ZipType.Zip: return zipParameters.ArchiveName + ".zip";
                case ZipType.Tar: return zipParameters.ArchiveName + ".tar";
                case ZipType.GZip: return zipParameters.ArchiveName + ".tar.gz";
                case ZipType.Bzip2: return zipParameters.ArchiveName + ".tar.bz2";
                default: throw new NotSupportedException("Unsupported archive type " + zipParameters.Type);
            }
        }

        private static IEnumerable<(string Source, string Name)> Entries(string path)
        {
            var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
            return Directory.Exists(path) ? DirEntries(path, name + "/") : new[] { (path, name) };
        }

        private static IEnumerable<(string Source, string Name)> DirEntries(string dir, string prefix)
        {
            return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                .Select(f => (f, prefix + Path.GetRelativePath(dir, f).Replace(Path.DirectorySeparatorChar, '/')));
        }

        // Entries are listed before the archive is created, so an archive written inside the source dir doesn't include itself.
        private static void Write(ZipParameters zipParameters, string password, List<(string Source, string Name)> entries)
        {
            var archivePath = ArchivePath(zipParameters);
            if (!string.IsNullOrEmpty(password) && zipParameters.Type != ZipType.Zip)
                throw new ArgumentException("Password protection is supported only for Zip archives");

            using var output = File.Create(archivePath);
            if (zipParameters.Type == ZipType.Zip)
            {
                using var zip = new ZipOutputStream(output);
                if (!string.IsNullOrEmpty(password))
                    zip.Password = password;
                foreach (var (source, name) in entries)
                {
                    var entry = new ZipEntry(name) { DateTime = File.GetLastWriteTime(source) };
                    if (!string.IsNullOrEmpty(password))
                        entry.AESKeySize = 256;
                    zip.PutNextEntry(entry);
                    using (var input = File.OpenRead(source))
                        input.CopyTo(zip);
                    zip.CloseEntry();
                }
                return;
            }

            Stream compressed = zipParameters.Type switch
            {
                ZipType.GZip => new GZipOutputStream(output),
                ZipType.Bzip2 => new BZip2OutputStream(output),
                _ => output
            };
            using (compressed)
            using (var tar = new TarOutputStream(compressed, System.Text.Encoding.UTF8))
            {
                foreach (var (source, name) in entries)
                {
                    var entry = TarEntry.CreateTarEntry(name);
                    entry.Size = new FileInfo(source).Length;
                    entry.ModTime = File.GetLastWriteTimeUtc(source);
                    tar.PutNextEntry(entry);
                    using (var input = File.OpenRead(source))
                        input.CopyTo(tar);
                    tar.CloseEntry();
                }
            }
        }
    }
}
