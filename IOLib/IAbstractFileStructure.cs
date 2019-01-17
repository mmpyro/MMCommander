using System;
using System.IO;

namespace IOLib
{

    public interface IAbstractFileStructure
    {
        string Name {get;}
        string FullName { get;}
        string Ext {get; }
        DateTime CreationTime {get;}
        DateTime LastAccessTime { get;}
        DateTime LastModifyTime { get; }
        bool IsDirectory { get; }
        long Size { get;  }
        bool IsReadOnly { get;}
        FileAttributes Attributes { get; set; }

        void Delete();
        void Move(IAbstractFileStructure destinationDirectory);
        void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride);
        void OverrideCopy(IAbstractFileStructure destinationDirectory);
        void Rename(string newName);
    }
}