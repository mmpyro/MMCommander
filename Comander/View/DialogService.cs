using System.Collections.Generic;
using System.Threading.Tasks;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Dtos;
using IOLib;
using ZipLib;

namespace Comander.View
{
    public class DialogService : IDialogService
    {
        public const string SkipTrashConfirmKey = "skipTrashConfirm";
        private readonly IConfigReader _config;

        public DialogService(IConfigReader config = null)
        {
            _config = config;
        }

        public Task<bool> Confirm(string message) =>
            WindowHost.ShowDialog<bool>(() => new ConfirmWindow(message));

        public Task<string> AskText(string message, string initialText = "") =>
            WindowHost.ShowDialog<string>(() => new InputWindow(message, initialText));

        public Task<CopyRequest> AskCopy(CopyDialogModel model) =>
            WindowHost.ShowDialog<CopyRequest>(() => new CopyDialog(model));

        public Task<ConflictAnswer> AskConflict(Conflict conflict) =>
            WindowHost.ShowDialog<ConflictAnswer>(() => new ConflictDialog(conflict));

        public async Task<DeleteRequest> AskDelete(DeleteDialogModel model)
        {
            if (!model.Permanent && model.TrashUnavailable == null && _config?.GetSetting(SkipTrashConfirmKey) == "true")
                return new DeleteRequest { Permanent = false };
            var request = await WindowHost.ShowDialog<DeleteRequest>(() => new DeleteDialog(model));
            if (request?.DontAskAgain == true)
                _config?.SaveSetting(SkipTrashConfirmKey, "true");
            return request;
        }

        public void ShowProgress(TransferJob job) => WindowHost.Show(() => new ProgressDialog(job));

        public Task<ZipParameters> AskZip(string directory) =>
            WindowHost.ShowDialog<ZipParameters>(() => new ZipWindow(directory));

        public Task<UnZipRequest> AskUnzip(IMetadataFileStructure archive) =>
            WindowHost.ShowDialog<UnZipRequest>(() => new UnZipWindow(archive));

        public Task<IReadOnlyList<string>> GetClipboardFiles() => WindowHost.GetClipboardFiles();

        public Task SetClipboardFiles(IEnumerable<string> paths) => WindowHost.SetClipboardFiles(paths);
    }
}
