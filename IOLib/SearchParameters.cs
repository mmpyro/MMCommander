namespace IOLib
{
    public class SearchParameters
    {
        public string StartDirectoryPath { get; set; }
        public string SearchFrase { get; set; }
        public bool Recursive { get; set; }

        public SearchParameters(string startDirectoryPath, string searchFrase, bool recursive = false)
        {
            StartDirectoryPath = startDirectoryPath;
            SearchFrase = searchFrase;
            Recursive = recursive;
        }
    }
}