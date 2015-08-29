namespace IOLib
{
    public class SearchParameters
    {
        public IAbstractFileStructure StartDirectory { get; set; }
        public string SearchFrase { get; set; }
        public bool Recursive { get; set; }

        public SearchParameters(IAbstractFileStructure startDirectory, string searchFrase, bool recursive = false)
        {
            StartDirectory = startDirectory;
            SearchFrase = searchFrase;
            Recursive = recursive;
        }
    }
}