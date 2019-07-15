using System.Linq;
using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    class NameLikeParser : Parser
    {
        public override Unit Perform(string input, IAbstractFileStructure[] abstractFiles)
        {
            Regex regex = new Regex(@"name[\s]{1}like[\s]{1}", RegexOptions.IgnoreCase);
            string val = regex.Replace(input, string.Empty).ToLower();
            abstractFiles = abstractFiles.Where(t => t.Name.ToLower().Contains(val) || IsParentDirMetaDataStructure(t)).ToArray();
            return new Unit(abstractFiles);
        }
    }
}