using System.Text.RegularExpressions;
using IOLib;

namespace IOLinq
{
    abstract class Parser
    {
        protected string PerformString(string input)
        {
            return input.ToLower().Replace(" ", "");
        }

        public static bool IsExtIn(string input)
        {
            return Regex.IsMatch(input, @"ext\s*->\s*[(?]\s*[a-zA-Z0-9].*\s*[)?]");
        }

        public static bool IsExtEquals(string input)
        {
            return Regex.IsMatch(input, @"ext\s*[=]{2}\s*[a-zA-Z0-9].*");
        }

        public static bool IsNameEquals(string input)
        {
            return Regex.IsMatch(input, @"name\s*[=]{2}\s*[a-zA-Z0-9].*");
        }

        public static bool IsLikeNameParser(string input)
        {
            return Regex.IsMatch(input, @".*[\s]{1}like[\s]{1}.*");
        }

        public abstract Unit Perform(string input, IAbstractFileStructure[] abstractFiles);
    }
}