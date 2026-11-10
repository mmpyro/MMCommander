using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IOLib
{
    public abstract class AbstractFileStructure : IAbstractFileStructure
    {
        protected FileInfo _fileInfo;
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
            get { return false; }
        }
        public long Size
        {
            get { return _fileInfo.Length; }
        }
        public bool IsReadOnly
        {
            get { return _fileInfo.IsReadOnly; }
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

    public class FileStructure : AbstractFileStructure
    {

        public FileStructure(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public FileStructure(string fullName)
        {
            _fileInfo = new FileInfo(fullName);    
        }

        public override void Delete()
        {
            File.SetAttributes(FullName, FileAttributes.Normal);
            File.Delete(FullName);
        }

        public override void Move(IAbstractFileStructure destinationDirectory)
        {
            if (destinationDirectory.IsDirectory)
            {
                string destFileName = Path.Combine(destinationDirectory.FullName, Name);
                File.Move(FullName, destFileName);
                _fileInfo = new FileInfo(destFileName);
            }
        }

        // Keeps the source attributes; only a ReadOnly target is cleared so it can be overwritten (BUG-30).
        public override void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride)
        {
            if (!destinationDirectory.IsDirectory)
                throw new ArgumentException("Invalid arguments");
            string destinationPath = Path.Combine(destinationDirectory.FullName, Name);
            bool exists = IsFileExistInDestinationDir(destinationDirectory);
            if (exists && !allowOverride(Name, destinationPath))
                return;
            try
            {
                if (exists)
                    File.SetAttributes(destinationPath, FileAttributes.Normal);
                File.Copy(FullName, destinationPath, exists);
            }
            catch (IOException ex) when (OperatingSystem.IsWindows() && ex.Message.Contains("in use"))
            {
                CopyFileInUse(destinationDirectory);
            }
        }

        public override void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
            Copy(destinationDirectory, (_, _) => true);
        }

        public override void Rename(string newName)
        {
            string localization = FullName.Substring(0, FullName.Length - Name.Length);
            string destFileName = Path.Combine(localization, newName);
            File.Move(FullName, destFileName );
            _fileInfo = new FileInfo(destFileName);
        }

        public override bool Equals(object obj)
        {
            var file = obj as FileStructure;
            if (file != null)
            {
                return file.FullName.Equals(FullName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(FullName);
        }

        public override string ToString()
        {
            return FullName;
        }

        protected bool IsFileExistInDestinationDir(IAbstractFileStructure destinationDirectory)
        {
            if (destinationDirectory.IsDirectory)
            {
                return Directory.GetFiles(destinationDirectory.FullName).Count(t => Path.GetFileName(t).Equals(Name, StringComparison.OrdinalIgnoreCase)) > 0;
            }
            return false;
        }

        // cmd.exe copy fallback for files locked by another process; Windows only.
        private void CopyFileInUse(IAbstractFileStructure destinationDirectory)
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C copy \"" + FullName + "\" \"" + destinationDirectory.FullName + "\"";
            process.Start();
            process.WaitForExit();
            process.Close();
        }
    }
}
