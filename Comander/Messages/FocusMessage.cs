using Comander.CommanderIO;

namespace Comander.Messages
{
    public struct SetFocusMessage
    {
        public string GridName { get; set; }
        public IMetadataFileStructure SelectedFile { get; set; }
    }

    public struct FocusMessage
    {
        public string ManagerName { get; set; }
    }
}
