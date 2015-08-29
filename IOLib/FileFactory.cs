using System.Collections.Generic;
using System.IO;

namespace IOLib
{
    public interface IFileFactory
    {
        List<IAbstractFileStructure> CreateEmptyFileList();
        IAbstractFileStructure CreateFileMsg(string fullPath);
        IAbstractFileStructure CreateFileMsg(FileInfo fileInfo);
        IAbstractFileStructure CreateFileMsg(DirectoryInfo directoryInfo);
        IAbstractFileStructure GetDirectoryFromPath(string path);
    }

    public class FileFactory : IFileFactory
    {
        public List<IAbstractFileStructure> CreateEmptyFileList()
        {
            return new List<IAbstractFileStructure>();
        }

        public IAbstractFileStructure CreateFileMsg(string fullPath)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(fullPath)))
            {
                return CreateFileMsg(new DirectoryInfo(fullPath));
            }
            return CreateFileMsg(new FileInfo(fullPath));
        }

        public  IAbstractFileStructure CreateFileMsg(FileInfo fileInfo)
        {
            return new FileStructure(fileInfo);
        }

        public IAbstractFileStructure CreateFileMsg(DirectoryInfo directoryInfo)
        {
            return new DirStructure(directoryInfo, this);
        }

        public  IAbstractFileStructure GetDirectoryFromPath(string path)
        {
            return new DirStructure(new DirectoryInfo(path), this);
        }
    }

}