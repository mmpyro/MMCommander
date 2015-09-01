using System;
using System.IO;

namespace IOLib
{
    public abstract class DirAbstractStructure : IAbstractFileStructure
    {
        protected DirectoryInfo _fileInfo;
        public string Name
        {
            get { return _fileInfo.Name; }
        }
        public string FullName
        {
            get { return _fileInfo.FullName; }
        }
        public string Ext
        {
            get
            {
                return _fileInfo.Extension;
            }
        }
        public DateTime CreationTime
        {
            get
            {
                return _fileInfo.CreationTime;
            }
        }
        public DateTime LastAccessTime
        {
            get
            {
                return _fileInfo.LastAccessTime;
            }
        }
        public DateTime LastModifyTime
        {
            get
            {
                return _fileInfo.LastWriteTime;
            }
        }
        public bool IsDirectory
        {
            get { return true; }
        }
        public long Size
        {
            get { return 0; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public FileAttributes Attributes
        {
            get
            {
                return _fileInfo.Attributes;
            }
            set { _fileInfo.Attributes = value; }
        }

        public abstract void Delete();
        public abstract void Move(IAbstractFileStructure destinationDirectory);
        public abstract void Copy(IAbstractFileStructure destinationDirectory, Func<string, bool> allowOverride);
        public abstract void OverrideCopy(IAbstractFileStructure destinationDirectory);
        public abstract void Rename(string newName);
    }

    public class DirStructure : DirAbstractStructure
    {
        private readonly IFileFactory _fileFactory;

        public DirStructure(string path, IFileFactory fileFactory)
        {
            _fileFactory = fileFactory;
            _fileInfo = new DirectoryInfo(path);
        }

        public DirStructure(DirectoryInfo directoryInfo, IFileFactory fileFactory)
        {
            _fileFactory = fileFactory;
            _fileInfo = directoryInfo;
        }

        protected void Init(string directoryPath)
        {
            _fileInfo = new DirectoryInfo(directoryPath);
        }

        public override void Delete()
        {
            Directory.Delete(FullName, true);
        }

        public override void Move(IAbstractFileStructure destinationDirectory)
        {
            string newPath = Path.Combine(destinationDirectory.FullName, Name);
            Directory.Move(FullName, newPath);
            Init(newPath);
        }

        public override void Copy(IAbstractFileStructure destinationDirectory, Func<string, bool> allowOverride)
        {
            string destinationPath = Path.Combine(destinationDirectory.FullName, Name);
            Directory.CreateDirectory(destinationPath);
            foreach (var file in Directory.GetFiles(FullName))
            {
                var fileInfo = new FileInfo(file);
                File.Copy(file, Path.Combine(destinationPath, fileInfo.Name), true);
            }
            foreach (var dir in Directory.GetDirectories(FullName))
            {
                var dirStructure = _fileFactory.CreateFileMsg(dir);
                dirStructure.Copy(_fileFactory.CreateFileMsg(destinationPath), (s) => true);
            }
        }

        public override void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
            string destinationPath = Path.Combine(destinationDirectory.FullName, Name);
            Directory.CreateDirectory(destinationPath);
            foreach (var file in Directory.GetFiles(FullName))
            {
                var fileInfo = new FileInfo(file);
                File.Copy(file, Path.Combine(destinationPath, fileInfo.Name), true);
            }
            foreach (var dir in Directory.GetDirectories(FullName))
            {
                var dirStructure = _fileFactory.CreateFileMsg(dir);
                dirStructure.OverrideCopy(_fileFactory.CreateFileMsg(destinationPath));
            }
        }

        public override void Rename(string newName)
        {
            string localization = FullName.Substring(0, FullName.Length - Name.Length);
            string newNamePath = Path.Combine(localization, newName);
            Directory.Move(FullName, newNamePath);
            Init(newNamePath);
        }
    }
    
}