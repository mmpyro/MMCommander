using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommanderPlugin
{
    // Any public class with a parameterless constructor that implements this is picked up from Plugins/.
    public interface ICommanderPlugin
    {
        /// <summary>Shown in the plugin list (Ctrl+P).</summary>
        string Name { get; }

        /// <param name="dir">Actual directory full name</param>
        /// <param name="files">Selected files full name</param>
        Task<Response> InvokeAsync(string dir, IEnumerable<string> files);
    }
}
