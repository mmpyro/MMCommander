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

        public override void Delete()
        {
            //do nothing
        }

        public override void Rename(string newName)
        {
            //do nothing
        }
    }
}
