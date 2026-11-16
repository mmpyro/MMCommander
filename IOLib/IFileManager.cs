using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IOLib
{
    public enum FileSizeUnit : long
    {
        B = 1,
        KB = 1024,
        MB = KB*KB,
        GB = KB*KB*KB
    }

    public interface IFileManager
    {
        void CreateFile(string path);
        void Split(IAbstractFileStructure abstractFile, long partSize, FileSizeUnit unit, string destinationPath);
        void Join(string sourcePath, string fileName, string destinationFileName);
    }

    public class FileManager : IFileManager
    {
        private readonly IFileFactory _fileFactory;

        public FileManager(IFileFactory fileFactory)
        {
            _fileFactory = fileFactory;
        }

        // long: part sizes and offsets pass 2 GB (BUG-07).
        private static long ConvertFileSize(long size, FileSizeUnit unit)
        {
            return checked(size*(long)unit);
        }

        private const int BufferSize = 81920;

        private void DevideFile(IAbstractFileStructure abstractFile, long size, FileSizeUnit unit, string destinationPath, int index)
        {
            byte[] buffer = new byte[BufferSize];
            long partSize = ConvertFileSize(size, unit);
            using (Stream input = File.OpenRead(abstractFile.FullName))
            {
                input.Position = input.Seek(partSize*index, SeekOrigin.Begin);
                using (Stream output = File.Create(Path.Combine(destinationPath, abstractFile.Name + "__" + index)))
                {
                    long remaining = partSize;
                    int bytesRead;
                    while (remaining > 0 && (bytesRead = input.Read(buffer, 0,
                            (int)Math.Min(remaining, BufferSize))) > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                        remaining -= bytesRead;
                    }
                }
            }
        }

        public void CreateFile(string path)
        {
            // CreateNew: an existing file throws instead of being emptied.
            using (new FileStream(path, FileMode.CreateNew)) { }
            File.SetAttributes(path, FileAttributes.Normal);
        }

        /// <summary>
        /// Devide file into smaller pices
        /// </summary>
        /// <param name="abstractFilehich will be devided</param>
        /// <param name="partSize">Size of file pice</param>
        /// <param name="unit">File size unit</param>
        public void Split(IAbstractFileStructure abstractFile, long partSize, FileSizeUnit unit, string destinationPath)
        {
            if (partSize <= 0 || ConvertFileSize(partSize, unit) >= abstractFile.Size)
            {
                throw new ArgumentException("Split size must be smaller than the file size");
            }
            else
            {
                int numberofParts = (int)Math.Ceiling((double)abstractFile.Size / ConvertFileSize(partSize, unit));
                Parallel.For(0, numberofParts, (i) => DevideFile(abstractFile, partSize, unit, destinationPath, i));

            }
        }

        // "movie.bin__0" -> "movie.bin": the name Join expects, from any part the user picked.
        public static string PartBaseName(string partPath) => Regex.Replace(Path.GetFileName(partPath), @"__\d+$", "");

        /// <summary>
        /// Join file which was splited
        /// </summary>
        /// <param name="sourcePath">Source directory path without file name</param>
        /// <param name="fileName">File name which was splited without index</param>
        /// <param name="destinationFileName">Destination full path with extension</param>
        public void Join(string sourcePath, string fileName, string destinationFileName)
        {
            // Only "<name>__<n>" parts of this file, in numeric order (BUG-15).
            var part = new Regex("^" + Regex.Escape(fileName) + @"__(\d+)$");
            var parts = Directory.GetFiles(sourcePath)
                .Select(path => (path, match: part.Match(Path.GetFileName(path))))
                .Where(t => t.match.Success)
                .Select(t => (t.path, index: long.Parse(t.match.Groups[1].Value)))
                .OrderBy(t => t.index)
                .ToList();
            if (parts.Count < 2)
                throw new ArgumentException("Join need at least two files");
            // A gap would silently produce a broken file.
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].index != i)
                    throw new ArgumentException(string.Format("Part {0}__{1} is missing", fileName, i));
            using (FileStream output = File.Create(destinationFileName))
            {
                foreach (var file in parts)
                {
                    using (var input = File.OpenRead(file.path))
                        input.CopyTo(output);
                }
            }
        }
    }

}