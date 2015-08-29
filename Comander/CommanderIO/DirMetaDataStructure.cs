using System;
using System.IO;
using IOLib;

namespace Comander.CommanderIO
{
    public class DirMetaDataStructure : IMetadataFileStructure
    {
        private readonly IFileFactory _fileFactory;
        private DirectoryInfo _fileInfo;
        private DirStructure _filestructure;

        public DirMetaDataStructure()
        {
            MetaData = new MetaData();
        }

        public DirMetaDataStructure(string path, IFileFactory fileFactory) : this()
        {
            _fileFactory = fileFactory;
            Init(new DirectoryInfo(path));
        }

        public DirMetaDataStructure(DirectoryInfo directoryInfo, IFileFactory fileFactory) : this()
        {
            _fileFactory = fileFactory;
            Init(directoryInfo);
        }

        private void Init(DirectoryInfo fileInfo)
        {
            _fileInfo = fileInfo;
            _filestructure = new DirStructure(fileInfo, _fileFactory);
            Name = fileInfo.Name;
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
            _filestructure.Delete();
        }

        public void Move(IAbstractFileStructure destinationDirectory)
        {
            _filestructure.Move(destinationDirectory);
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string, bool> allowOverride)
        {
            _filestructure.Copy(destinationDirectory, allowOverride);
        }

        public void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
           _filestructure.OverrideCopy(destinationDirectory);
        }

        public void Rename(string newName)
        {
            _filestructure.Rename(newName);
        }

        public MetaData MetaData { get; set; }
        public bool IsSelected()
        {
            return MetaData.IsSelected;
        }

        public void SelectFile()
        {
            MetaData.IsSelected = !MetaData.IsSelected;
        }

        public void PermanentSelect()
        {
            MetaData.IsSelected = true;
        }

        public void SetColor(string color)
        {
            MetaData.Color = color;
        }
    }
}