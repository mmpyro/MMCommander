using Comander.CommanderIO;

namespace Comander.Messages
{
    public class SetFocusMessage
    {
        public string GridName { get; set; }
        public IMetadataFileStructure SelectedFile { get; set; }
    }

    public class FocusMessage
    {
        public string ManagerName { get; set; }
    }
}
