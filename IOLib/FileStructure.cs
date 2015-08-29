using System;
using System.IO;
using System.Linq;

namespace IOLib
{
    public class FileStructure : IAbstractFileStructure
    {
        private FileInfo _fileInfo;

        public FileStructure(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public FileStructure(string fullName)
        {
            Init(fullName);       
        }

        public void Delete()
        {
            File.SetAttributes(FullName, FileAttributes.Normal);
            File.Delete(FullName);
        }

        public void Move(IAbstractFileStructure destinationDirectory)
        {
            if (destinationDirectory.IsDirectory)
            {
                string destFileName = Path.Combine(destinationDirectory.FullName, Name);
                File.Move(FullName, destFileName);
                Init(destFileName);
            }
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string, bool> allowOverride)
        {
            if (destinationDirectory.IsDirectory)
            {
                string destinationPath = Path.Combine(destinationDirectory.FullName, Name);
                if (IsFileExistInDestinationDir(destinationDirectory))
                {
                    if (allowOverride(Name))
                    {
                        File.Copy(FullName, destinationPath, true);
                        File.SetAttributes(destinationPath, FileAttributes.Normal);
                    }
                }
                else
                {
                    File.Copy(FullName, destinationPath);
                    File.SetAttributes(destinationPath, FileAttributes.Normal);
                }
            }
            else
            {
                throw new ArgumentException("Invalid arguments");
            }
        }

        public void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
            if (destinationDirectory.IsDirectory)
            {
                string destinationPath = Path.Combine(destinationDirectory.FullName, Name);
                File.Copy(FullName, destinationPath, true);
                File.SetAttributes(destinationPath, FileAttributes.Normal);
            }
            else
            {
                throw new ArgumentException("Invalid arguments");
            }
        }

        public void Rename(string newName)
        {
            string localization = FullName.Substring(0, FullName.Length - Name.Length);
            string destFileName = Path.Combine(localization, newName);
            File.Move(FullName, destFileName );
            Init(destFileName);
        }
        #region newProperty

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
        #endregion

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
            return FullName.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        protected void Init(String filePath)
        {
            _fileInfo = new FileInfo(filePath);
        }

        protected bool IsFileExistInDestinationDir(IAbstractFileStructure destinationDirectory)
        {
            if (destinationDirectory.IsDirectory)
            {
                return Directory.GetFiles(destinationDirectory.FullName).Count(t => Path.GetFileName(t).Equals(Name, StringComparison.OrdinalIgnoreCase)) > 0;
            }
            return false;
        }


    }
}
