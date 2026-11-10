using System.Collections.Generic;
using System.Threading.Tasks;
using Comander.CommanderIO;
using Comander.Dtos;
using ZipLib;

namespace Comander.Core
{
    public enum ExtractType
    {
        Here,
        ToSpecificDir
    }

    public class UnZipRequest
    {
        public ZipParameters Parameters { get; set; }
        public ExtractType ExtractType { get; set; }
    }

    // Every question ProxyIO asks the user. Implementations marshal to the UI thread, so callers may be on any thread.
    public interface IDialogService
    {
        Task<bool> Confirm(string message);

        /// <returns>The entered text, or null when cancelled.</returns>
        Task<string> AskText(string message, string initialText = "");

        Task<CopyOption> AskOverride(string message);

        /// <returns>Null when cancelled.</returns>
        Task<ZipParameters> AskZip(string directory);

        /// <returns>Null when cancelled.</returns>
        Task<UnZipRequest> AskUnzip(IMetadataFileStructure archive);

        Task<IReadOnlyList<string>> GetClipboardFiles();
        Task SetClipboardFiles(IEnumerable<string> paths);
    }
}
