using System.Collections.Generic;
using IOLib;

namespace CommanderPlugin
{
    public interface ICommanderPlugin
    {
        void Invoke(IEnumerable<IAbstractFileStructure> files);
    }
}