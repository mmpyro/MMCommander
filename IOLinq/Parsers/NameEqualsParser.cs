using System;
using System.Linq;
using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    class NameEqualsParser : Parser
    {
        private string RemoveExt(string fileName)
        {
            return Regex.Replace(fileName, @"\..*", "");
        }

        public override Unit Perform(string input, IAbstractFileStructure[] abstractFiles)
        {
            Regex regex = new Regex(@"name\s*[=]{2}\s*", RegexOptions.IgnoreCase);
            string val = regex.Replace(input, string.Empty).ToLower();
            abstractFiles = abstractFiles.Where(t => RemoveExt(t.Name).Equals(val, StringComparison.OrdinalIgnoreCase)).ToArray();
            return new Unit(abstractFiles);
        }
    }
}