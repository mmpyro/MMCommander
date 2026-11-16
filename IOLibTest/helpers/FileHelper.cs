using System;
using System.IO;
using IOLib;

namespace IOLibTest.helpers
{
    public static class FileHelper
    {
        public static void DeleteIfExist(string path)
        {
            if(File.Exists(path))
                File.Delete(path);
        }

        public static void CreateIfNotExist(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path))
            {
                var stream = File.Create(path);
                stream.Close();
            }
        }

        public static bool Exist(string path)
        {
            return File.Exists(path);
        }

        public static void DeleteDirIfExist(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path,true);
            }
        }

        public static void CreateDirIfNotExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void DeleteAllFilesInDirectory(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            foreach (var file in directoryInfo.GetFiles())
            {
                File.Delete(file.FullName);
            }
        }

        public static void CreateFileWithCertainSize(string path, int size, FileSizeUnit unit)
        {
            int filesize = 0;
            string str = null;
            const int charSize = 16;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            size = size*charSize;
            switch (unit)
            {
                case FileSizeUnit.B:
                    filesize = size/16;
                    str = new string('a', filesize);
                    File.WriteAllText(path, str);
                    break;
                case FileSizeUnit.KB:
                    filesize = (size*(int)FileSizeUnit.KB)/16;
                    str = new string('a', filesize);
                    File.WriteAllText(path, str);
                    break;
                default:
                    throw new ArgumentException("Invalid size unit");
            }
        }

    }
}