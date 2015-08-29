using IOLib;

namespace Comander.CommanderIO
{
    public interface IMetadataFileStructure : IAbstractFileStructure
    {
        MetaData MetaData { get; set; }
        bool IsSelected();
        void SelectFile();
        void PermanentSelect();
        void SetColor(string color);
    }
}