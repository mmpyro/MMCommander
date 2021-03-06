﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace IOLib.Comparators
{
    public class FileNameComparer : IComparer<IAbstractFileStructure>
    {
        private readonly StringComparer _comparer = StringComparer.Create(new CultureInfo("en"), true);


        public int Compare(IAbstractFileStructure x, IAbstractFileStructure y)
        {
            return _comparer.Compare((string) x.Name, (string) y.Name);
        }
    }
}