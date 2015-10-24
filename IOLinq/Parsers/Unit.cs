using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOLib;

namespace IOLinq
{
    class Unit 
    {
        private readonly IAbstractFileStructure[] _abstractFiles;

        public Unit(IAbstractFileStructure[] abstractFiles)
        {
            _abstractFiles = abstractFiles;
        }

        public IAbstractFileStructure[] AbstractFiles
        {
            get { return _abstractFiles; }
        }

        public static Unit operator |(Unit ext1, Unit ext2)
        {
            HashSet<IAbstractFileStructure> set1 = new HashSet<IAbstractFileStructure>(ext1.AbstractFiles);
            HashSet<IAbstractFileStructure> set2 = new HashSet<IAbstractFileStructure>(ext2.AbstractFiles);

            return new Unit(set1.Union(set2).ToArray());
        }

        public static Unit operator &(Unit ext1, Unit ext2)
        {
            HashSet<IAbstractFileStructure> set1 = new HashSet<IAbstractFileStructure>(ext1.AbstractFiles);
            HashSet<IAbstractFileStructure> set2 = new HashSet<IAbstractFileStructure>(ext2.AbstractFiles);

            return new Unit(set1.Intersect(set2).ToArray());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IAbstractFileStructure item in AbstractFiles)
            {
                sb.Append(item).Append("\n");
            }
            return sb.ToString();
        }
    }
}