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
        FileType Type { get; }

        void Delete();
        void Rename(string newName);
    }

}