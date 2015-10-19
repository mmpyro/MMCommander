using System;
using System.IO;

namespace IOLib
{

    public interface IFileStructure
    {
        string Name { get; }
        string FullName { get; }
        string Ext { get; }
        DateTime CreationTime { get; }
        DateTime LastAccessTime { get; }
        DateTime LastModifyTime { get; }
        bool IsDirectory { get; }
        long Size { get; }
        bool IsReadOnly { get; }
        FileAttributes Attributes { get; set; }
        string GetDirectoryPath();
        string GetRootPath();

        void Delete();
        void Move(IFileStructure destinationDirectory);
        void Copy(IFileStructure destinationDirectory, Func<string, bool> allowOverride);
        void OverrideCopy(IFileStructure destinationDirectory);
        void Rename(string newName);
        void Refresh();
    }
}