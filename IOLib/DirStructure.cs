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
        public abstract void Move(IAbstractFileStructure destinationDirectory);
        public abstract void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride);
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
            ClearAttributes(FullName);
            Directory.Delete(FullName, true);
        }

        public override void Move(IAbstractFileStructure destinationDirectory)
        {
            string newPath = Path.Combine(destinationDirectory.FullName, Name);
            Directory.Move(FullName, newPath);
            Init(newPath);
        }

        // Each nested file goes through FileStructure.Copy, so allowOverride is asked for every conflict (BUG-01).
        public override void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride)
        {
            if (IsSameOrInside(destinationDirectory.FullName, FullName))
                throw new IOException(string.Format("Cannot copy {0} into itself ({1})", FullName, destinationDirectory.FullName));
            string destinationPath = Path.Combine(destinationDirectory.FullName, Name);
            Directory.CreateDirectory(destinationPath);
            var destination = _fileFactory.CreateFileMsg(destinationPath);
            foreach (var file in Directory.GetFiles(FullName))
                _fileFactory.CreateFileMsg(file).Copy(destination, allowOverride);
            foreach (var dir in Directory.GetDirectories(FullName))
                _fileFactory.CreateFileMsg(dir).Copy(destination, allowOverride);
        }

        public override void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
            Copy(destinationDirectory, (_, _) => true);
        }

        // BUG-16: true when path is dir itself or anywhere below it.
        private static bool IsSameOrInside(string path, string dir)
        {
            var relative = Path.GetRelativePath(dir, path);
            return relative == "." || !(relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar) || Path.IsPathRooted(relative));
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