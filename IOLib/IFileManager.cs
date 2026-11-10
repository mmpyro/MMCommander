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
        bool FileExist(string path);
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

        private int CreateBufferSize(FileSizeUnit unit)
        {
            switch (unit)
            {
                case FileSizeUnit.KB:
                    return 10240;
                case FileSizeUnit.MB:
                    return 20*1024;
                default:
                    return 100;
            }
        }

        private int CreateBufferSize(long fileSize)
        {
            if(fileSize > (int)FileSizeUnit.MB)
                return 20 * 1024;
            else if (fileSize > (int) FileSizeUnit.KB)
                return 10240;
            return 100;
        }

        private void DevideFile(IAbstractFileStructure abstractFile, long size, FileSizeUnit unit, string destinationPath, int index)
        {
            int BUFFER_SIZE = CreateBufferSize(unit);
            byte[] buffer = new byte[BUFFER_SIZE];
            long partSize = ConvertFileSize(size, unit);
            using (Stream input = File.OpenRead(abstractFile.FullName))
            {
                input.Position = input.Seek(partSize*index, SeekOrigin.Begin);
                using (Stream output = File.Create(Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(abstractFile.Name) + "__" + index)))
                {
                    long remaining = partSize;
                    int bytesRead;
                    while (remaining > 0 && (bytesRead = input.Read(buffer, 0,
                            (int)Math.Min(remaining, BUFFER_SIZE))) > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                        remaining -= bytesRead;
                    }
                }
            }
        }

        private void JoinFile(string fullfileName,FileStream fileStream)
        {
            long partSize = new FileInfo(fullfileName).Length;
            int BUFFER_SIZE = CreateBufferSize(partSize);
            byte[] buffer = new byte[BUFFER_SIZE];
            using (Stream input = File.OpenRead(fullfileName))
            {
                fileStream.Position = fileStream.Seek(fileStream.Length, SeekOrigin.Begin);
                long remaining = partSize;
                int bytesRead;
                while (remaining > 0 && (bytesRead = input.Read(buffer, 0,
                        (int)Math.Min(remaining, BUFFER_SIZE))) > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                    remaining -= bytesRead;
                }
            }
        }

        public void CreateFile(string path)
        {
            var stream = File.Create(path);
            stream.Close();
            File.SetAttributes(path, FileAttributes.Normal);
        }

        public bool FileExist(string path)
        {
            return File.Exists(path);
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
                throw new ArgumentException("Split size can'AbstractFileStructure be grater than file size");
            }
            else
            {
                int numberofParts = (int)Math.Ceiling((double)abstractFile.Size / ConvertFileSize(partSize, unit));
                Parallel.For(0, numberofParts, (i) => DevideFile(abstractFile, partSize, unit, destinationPath, i));

            }
        }

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
                .OrderBy(t => long.Parse(t.match.Groups[1].Value))
                .Select(t => t.path)
                .ToList();
            if (parts.Count < 2)
                throw new ArgumentException("Join need at least two files");
            using (FileStream fileStream = File.Create(destinationFileName))
            {
                foreach (string file in parts)
                {
                    JoinFile(file, fileStream);
                }
            }
        }
    }

}