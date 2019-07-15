using System;
using System.IO;
using IOLib;

namespace Comander.CommanderIO
{
    public class FileMetaDataStructure : IMetadataFileStructure
    {
        protected FileInfo _fileInfo;
        private FileStructure _filestructure;

        private void Init(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
            _filestructure = new FileStructure(fileInfo);
            Name = fileInfo.Name;
            FullName = fileInfo.FullName;
            Ext = fileInfo.Extension;
            CreationTime = fileInfo.CreationTime;
            LastAccessTime = fileInfo.LastAccessTime;
            LastModifyTime = fileInfo.LastWriteTime;
            IsDirectory = false;
            Size = fileInfo.Length;
            IsReadOnly = fileInfo.IsReadOnly;
            Attributes = fileInfo.Attributes;
        }

        public FileMetaDataStructure()
        {
            MetaData = new MetaData();
        }

        public FileMetaDataStructure(string fullName) : this()
        {
            Init(new FileInfo(fullName));
        }

        public FileMetaDataStructure(FileInfo fileInfo) : this()
        {
            Init(fileInfo);
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
            if (destinationDirectory.IsDirectory)
            {
                string destFileName = Path.Combine(destinationDirectory.FullName, Name);
                File.Move(FullName, destFileName);
            }
        }

        public  void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride)
        {
            _filestructure.Copy(destinationDirectory, allowOverride);  
        }

        public  void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
           _filestructure.OverrideCopy(destinationDirectory);
        }

        public  void Rename(string newName)
        {
            string localization = FullName.Substring(0, FullName.Length - Name.Length);
            string destFileName = Path.Combine(localization, newName);
            File.Move(FullName, destFileName);
        }

        public override bool Equals(object obj)
        {
            var metaData = obj as FileMetaDataStructure;
            if (metaData != null)
            {
                return _filestructure.Equals(metaData._filestructure);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _filestructure.GetHashCode();
        }

        public override string ToString()
        {
            return _filestructure.ToString();
        }

        public MetaData MetaData { get; set; }

        public FileType Type => FileType.Normal;

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