using IOLib;

namespace Search
{
    public interface IFileNotifier
    {
        void Notify(IAbstractFileStructure file);
    }
}