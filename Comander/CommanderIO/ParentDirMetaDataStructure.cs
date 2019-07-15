using IOLib;
using System;
using System.IO;

namespace Comander.CommanderIO
{
    public class ParentDirMetaDataStructure : IMetadataFileStructure
    {
        private DirectoryInfo _fileInfo;
        private ParentDirStructure _filestructure;

        public ParentDirMetaDataStructure()
        {
            MetaData = new MetaData();
        }

        public ParentDirMetaDataStructure(string path) : this()
        {
            Init(new DirectoryInfo(path));
        }

        public ParentDirMetaDataStructure(DirectoryInfo directoryInfo) : this()
        {
            Init(directoryInfo);
        }

        private void Init(DirectoryInfo fileInfo)
        {
            _fileInfo = fileInfo;
            _filestructure = new ParentDirStructure(fileInfo);
            Name = "..";
            FullName = fileInfo.FullName;
            Ext = fileInfo.Extension;
            CreationTime = fileInfo.CreationTime;
            LastAccessTime = fileInfo.LastAccessTime;
            LastModifyTime = fileInfo.LastWriteTime;
            IsDirectory = true;
            Size = 0;
            IsReadOnly = false;
            Attributes = fileInfo.Attributes;
        }

        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string Ext { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastModifyTime { get; private set; }
        public bool IsDirectory { get; private set; }
        public long Size { get; private set; }
        public bool IsReadOnly { get; private set; }
        public FileAttributes Attributes { get; set; }

        public void Delete()
        {
            //do nothing
        }

        public void Move(IAbstractFileStructure destinationDirectory)
        {
            //do nothing
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride)
        {
            //do nothing
        }

        public void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
            //do nothing
        }

        public void Rename(string newName)
        {
            //do nothing
        }

        public MetaData MetaData { get; set; }

        public FileType Type => FileType.Special;

        public bool IsSelected()
        {
            return false;
        }

        public void SelectFile()
        {
            //do nothing
        }

        public void PermanentSelect()
        {
            //do nothing
        }

        public void SetColor(string color)
        {
            //do nothing
        }
    }
}
