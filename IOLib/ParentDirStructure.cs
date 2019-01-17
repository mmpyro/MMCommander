using System;
using System.IO;

namespace IOLib
{
    public class ParentDirStructure : DirAbstractStructure
    {
        public ParentDirStructure(string path)
        {
            _fileInfo = new DirectoryInfo(path);
        }

        public ParentDirStructure(DirectoryInfo directoryInfo)
        {
            _fileInfo = directoryInfo;
        }

        public override string Name
        {
            get
            {
                return "..";
            }
        }

        public override void Copy(IAbstractFileStructure destinationDirectory, Func<string,string, bool> allowOverride)
        {
            //do nothing
        }

        public override void Delete()
        {
            //do nothing
        }

        public override void Move(IAbstractFileStructure destinationDirectory)
        {
            //do nothing
        }

        public override void OverrideCopy(IAbstractFileStructure destinationDirectory)
        {
            //do nothing
        }

        public override void Rename(string newName)
        {
            //do nothing
        }
    }
}
