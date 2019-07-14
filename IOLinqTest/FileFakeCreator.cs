using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IOLib;
using NUnit.Framework;

namespace IOLinqTest
{
    public class FakeAbstractFile : IAbstractFileStructure
    {
        public FakeAbstractFile(string name, string ext)
        {
            Name = name;
            Ext = ext;
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
        }

        public void Move(IAbstractFileStructure destinationDirectory)
        {
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string, bool> allowOverride)
        {
        }

        public void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
        }

        public void Rename(string newName)
        {
        }

        public void Copy(IAbstractFileStructure destinationDirectory, Func<string, string, bool> allowOverride)
        {
        }
    }

    public class FileFakeCreator 
    {
        public  FakeAbstractFile CreateFile(string[] parameters)
        {
            return new FakeAbstractFile(parameters[0], parameters[1]);
        }

        public List<FakeAbstractFile> CreateFiles(string[][] names)
        {
            return names.Select(CreateFile).ToList();
        }
    }
}