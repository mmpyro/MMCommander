using System.Linq;
using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    class IsTypeParser : Parser
    {
        public override Unit Perform(string input, IAbstractFileStructure[] abstractFiles)
        {
            input = PerformString(input);
            Regex regex = new Regex(@"type\s*is\s*", RegexOptions.IgnoreCase);
            string type = regex.Replace(input, string.Empty).ToLower();
            if (type == "dir")
            {
                abstractFiles = abstractFiles.Where(t => t.IsDirectory || IsParentDirMetaDataStructure(t)).ToArray();
            }
            else if(type == "file")
            {
                abstractFiles = abstractFiles.Where(t => !t.IsDirectory || IsParentDirMetaDataStructure(t)).ToArray();
            }
            return new Unit(abstractFiles);
        }
    }
}
