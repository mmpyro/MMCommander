using IOLib;

namespace Comander.Messages
{
    public class SearchMessage
    {
        public SearchParameters SearchParameters { get; set; }
        public object SearchOptions { get; set; }
    }
}
