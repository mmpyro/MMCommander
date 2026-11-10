using System.Collections.Generic;
using System.Threading.Tasks;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Dtos;
using ZipLib;

namespace Comander.View
{
    public class DialogService : IDialogService
    {
        public Task<bool> Confirm(string message) =>
            WindowHost.ShowDialog<bool>(() => new ConfirmWindow(message));

        public Task<string> AskText(string message, string initialText = "") =>
            WindowHost.ShowDialog<string>(() => new InputWindow(message, initialText));

        public Task<CopyOption> AskOverride(string message) =>
            WindowHost.ShowDialog<CopyOption>(() => new OverrideWindow(message));

        public Task<ZipParameters> AskZip(string directory) =>
            WindowHost.ShowDialog<ZipParameters>(() => new ZipWindow(directory));

        public Task<UnZipRequest> AskUnzip(IMetadataFileStructure archive) =>
            WindowHost.ShowDialog<UnZipRequest>(() => new UnZipWindow(archive));

        public Task<IReadOnlyList<string>> GetClipboardFiles() => WindowHost.GetClipboardFiles();

        public Task SetClipboardFiles(IEnumerable<string> paths) => WindowHost.SetClipboardFiles(paths);
    }
}
