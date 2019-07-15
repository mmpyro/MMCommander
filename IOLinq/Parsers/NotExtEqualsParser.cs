using System.Linq;
using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    class NotExtEqualsParser : Parser
    {
        public override Unit Perform(string input, IAbstractFileStructure[] abstractFiles)
        {
            input = PerformString(input);
            Regex regex = new Regex(@"ext\s*[!][=]\s*", RegexOptions.IgnoreCase);
            string val = regex.Replace(input, string.Empty);
            abstractFiles = abstractFiles.Where(t => t.Ext.ToLower().Replace(".", string.Empty).Equals(val) == false || IsParentDirMetaDataStructure(t)).ToArray();
            return new Unit(abstractFiles);
        }
    }
}