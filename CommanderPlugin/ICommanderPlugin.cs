using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommanderPlugin
{
    public interface ICommanderPlugin
    {

        /// <param name="dir">Actual directory full name</param>
        /// <param name="files">Selected files full name</param>
        Task<Response> InvokeAsync(string dir, IEnumerable<string> files);
    }
}