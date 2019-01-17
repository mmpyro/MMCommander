using System.Text.RegularExpressions;

namespace IOLib
{
    public class SearchParameters
    {
        public string StartDirectoryPath { get; set; }
        public string SearchPhrase { get; set; }
        public bool Recursive { get; set; }
        public MatchOptions MatchOptions { get; set; }
        public RegexOptions RegexOptions { get; set; }

        public SearchParameters(string startDirectoryPath, string searchFrase, bool recursive = false)
        {
            StartDirectoryPath = startDirectoryPath;
            SearchPhrase = searchFrase;
            Recursive = recursive;
        }
    }
}