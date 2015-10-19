using System.Linq;
using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    class ExtNotInParser : Parser
    {
        public override Unit Perform(string input, IAbstractFileStructure[] abstractFiles)
        {
            input = PerformString(input);
            Regex regex = new Regex(@"ext\s*<>\s*", RegexOptions.IgnoreCase);
            string[] val = { };
            val = regex.Replace(input, string.Empty).Replace("(", string.Empty).Replace(")", string.Empty).Split(',');
            for (int i = 0; i < val.Length; i++)
            {
                val[i] = PerformString(val[i]);
            }
            abstractFiles = abstractFiles.Where(t => !val.Contains(t.Ext.ToLower().Replace(".", string.Empty))).ToArray();
            return new Unit(abstractFiles);
        }
    }
}