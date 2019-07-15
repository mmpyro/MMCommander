using System.Linq;
using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    class ExtEqualsParser : Parser
    {

        public override Unit Perform(string input, IAbstractFileStructure[] abstractFiles)
        {
            input = PerformString(input);
            Regex regex = new Regex(@"ext\s*[=]{2}\s*", RegexOptions.IgnoreCase);
            string val = regex.Replace(input, string.Empty);
            abstractFiles = abstractFiles.Where(t => t.Ext.ToLower().Replace(".", string.Empty).Equals(val) || IsParentDirMetaDataStructure(t) ).ToArray();
            return new Unit(abstractFiles);
        }
    }
}