using System;
using System.Collections.Generic;

namespace IOLib.Comparators
{
    public class FileTypeComparer : IComparer<IAbstractFileStructure>
    {

        public int Compare(IAbstractFileStructure x, IAbstractFileStructure y)
        {
            if (x.IsDirectory && y.IsDirectory)
                return 0;
            else if (x.IsDirectory && y.IsDirectory == false)
                return -1;
            return 1;
        }
    }
}