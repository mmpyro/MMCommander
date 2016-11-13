using System;
using System.IO;
using IOLib;

namespace IOLibTest.helpers
{
    public static class FileHelper
    {
        private const string _dir1 = "t1";
        private const string _dir2 = "t2";
        private const string _fileName = "file.dat";

        public static string Dir1()
        {
            return _dir1;
        }

        public static string Dir2()
        {
            return _dir2;
        }

        public static string FileName()
        {
            return _fileName;
        }

        public static void DeleteIfExist(string path)
        {
            if(File.Exists(path))
                File.Delete(path);
        }

        public static void CreateIfNotExist(string path)
        {
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

        public static void InitCopyFileTest(string directoryPath)
        {
            CreateDirIfNotExist(Path.Combine(directoryPath, _dir1));
            CreateDirIfNotExist(Path.Combine(directoryPath, _dir2));
            CreateIfNotExist(Path.Combine(directoryPath,_dir1,_fileName));
        }

        public static void CleanCopyFileTest(string directoryPath)
        {
            DeleteDirIfExist(Path.Combine(directoryPath, _dir1));
            DeleteDirIfExist(Path.Combine(directoryPath, _dir2));
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