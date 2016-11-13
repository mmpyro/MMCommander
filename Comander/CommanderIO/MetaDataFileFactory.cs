using System;
using System.Collections.Generic;
using System.IO;
using IOLib;

namespace Comander.CommanderIO
{
    public class MetaDataFileFactory : IFileFactory
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

        public IAbstractFileStructure CreateFileMsg(FileInfo fileInfo)
        {
            return new FileMetaDataStructure(fileInfo);
        }

        public IAbstractFileStructure CreateFileMsg(DirectoryInfo directoryInfo)
        {
            return new DirMetaDataStructure(directoryInfo, this);
        }

        public IAbstractFileStructure CreateParentDirectoryMsg(DirectoryInfo directoryInfo)
        {
            return new ParentDirMetaDataStructure(directoryInfo);
        }

        public IAbstractFileStructure GetDirectoryFromPath(string path)
        {
            return new DirMetaDataStructure(new DirectoryInfo(path), this);
        }
    }
}