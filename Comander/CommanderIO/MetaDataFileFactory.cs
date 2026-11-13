using System;
using System.Collections.Generic;
using System.IO;
using IOLib;

namespace Comander.CommanderIO
{
    public class MetaDataFileFactory : IFileFactory
    {
        public IAbstractFileStructure CreateFileMsg(string fullPath)
        {
            FileAttributes attr = File.GetAttributes(fullPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return CreateFileMsg(new DirectoryInfo(fullPath));
            }
            return CreateFileMsg(new FileInfo(fullPath));
        }

        public IAbstractFileStructure CreateFileMsg(FileInfo fileInfo)
        {
            return new FileMetaDataStructure(fileInfo);
        }

        public IAbstractFileStructure CreateFileMsg(DirectoryInfo directoryInfo)
        {
            return new DirMetaDataStructure(directoryInfo);
        }

        public IAbstractFileStructure CreateParentDirectoryMsg(DirectoryInfo directoryInfo)
        {
            return new ParentDirMetaDataStructure(directoryInfo);
        }

        public IAbstractFileStructure GetDirectoryFromPath(string path)
        {
            return new DirMetaDataStructure(new DirectoryInfo(path));
        }
    }
}