using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IOLib
{
    public abstract class DirAbstractStructure : IAbstractFileStructure
    {
        protected DirectoryInfo _fileInfo;
        public virtual string Name
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

        public FileType Type => FileType.Normal;

        public abstract void Delete();
        public abstract void Rename(string newName);
    }

    public class DirStructure : DirAbstractStructure
    {
        public DirStructure(string path)
        {
            _fileInfo = new DirectoryInfo(path);
        }

        public DirStructure(DirectoryInfo directoryInfo)
        {
            _fileInfo = directoryInfo;
        }

        protected void Init(string directoryPath)
        {
            _fileInfo = new DirectoryInfo(directoryPath);
        }

        public override void Delete()
        {
            ClearAttributes(FullName);
            Directory.Delete(FullName, true);
        }

        public override void Rename(string newName)
        {
            string localization = FullName.Substring(0, FullName.Length - Name.Length);
            string newNamePath = Path.Combine(localization, newName);
            Directory.Move(FullName, newNamePath);
            Init(newNamePath);
        }

        private static void ClearAttributes(string path)
        {
            var stack = new Stack<string>();
            stack.Push(path);
            while (stack.Count > 0)
            {
                var dir = new DirectoryInfo(stack.Pop());
                File.SetAttributes(dir.FullName, FileAttributes.Normal);
                dir.GetFiles().ToList().ForEach(f => File.SetAttributes(f.FullName, FileAttributes.Normal));
                dir.GetDirectories().ToList().ForEach(d => stack.Push(d.FullName));
            }
        }
    }
    
}