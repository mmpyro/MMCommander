using System.Collections.Generic;
using System.Threading.Tasks;
using IOLib;
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

        /// <returns>Null when cancelled.</returns>
        Task<CopyRequest> AskCopy(CopyDialogModel model);

        // Called from the transfer worker thread; implementations marshal to the UI thread.
        Task<ConflictAnswer> AskConflict(Conflict conflict);

        /// <returns>Null when cancelled.</returns>
        Task<DeleteRequest> AskDelete(DeleteDialogModel model);

        // Non-modal progress window for a queued job.
        void ShowProgress(TransferJob job);

        /// <returns>Null when cancelled.</returns>
        Task<ZipParameters> AskZip(string directory);

        /// <returns>Null when cancelled.</returns>
        Task<UnZipRequest> AskUnzip(IMetadataFileStructure archive);

        Task<IReadOnlyList<string>> GetClipboardFiles();
        Task SetClipboardFiles(IEnumerable<string> paths);
    }
}
