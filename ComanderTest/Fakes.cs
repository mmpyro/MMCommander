using IOLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Comander.CommanderIO;
using Comander.Core;
using Comander.Dtos;
using ZipLib;

namespace ComanderTest
{
    public class FakeDialogs : IDialogService
    {
        // Answer for conflicts; Skip unless a test says otherwise.
        public ConflictAnswer ConflictAnswer { get; set; } = new ConflictAnswer(ConflictDecision.Skip);
        public int ConflictQuestions { get; private set; }
        // Null = accept the dialog's defaults; set to change the policy etc.
        public Func<CopyDialogModel, CopyRequest> CopyAnswer { get; set; }
        public CopyDialogModel LastCopy { get; private set; }
        // Tests never touch the real Trash: delete permanently unless told otherwise.
        public DeleteRequest DeleteAnswer { get; set; } = new DeleteRequest { Permanent = true };
        public DeleteDialogModel LastDelete { get; private set; }

        public Task<bool> Confirm(string message) => Task.FromResult(true);
        public string TextAnswer { get; set; }
        public UnZipRequest UnzipAnswer { get; set; }

        public Task<string> AskText(string message, string initialText = "") => Task.FromResult(TextAnswer);
        public Task<CopyRequest> AskCopy(CopyDialogModel model)
        {
            LastCopy = model;
            return Task.FromResult(CopyAnswer != null ? CopyAnswer(model)
                : new CopyRequest { Destination = model.Destination, Options = new TransferOptions { Policy = model.Policy } });
        }

        public Task<ConflictAnswer> AskConflict(Conflict conflict)
        {
            ConflictQuestions++;
            return Task.FromResult(ConflictAnswer);
        }

        public Task<DeleteRequest> AskDelete(DeleteDialogModel model)
        {
            LastDelete = model;
            return Task.FromResult(DeleteAnswer);
        }

        public void ShowProgress(TransferJob job) { }
        public Task<ZipParameters> AskZip(string directory) => Task.FromResult<ZipParameters>(null);
        public Task<UnZipRequest> AskUnzip(IMetadataFileStructure archive) => Task.FromResult(UnzipAnswer);
        public Task<IReadOnlyList<string>> GetClipboardFiles() => Task.FromResult<IReadOnlyList<string>>(new List<string>());
        public Task SetClipboardFiles(IEnumerable<string> paths) => Task.CompletedTask;
    }

    public class FakePanel : IIOManager
    {
        public string ActualPath { get; set; }
        public ObservableCollection<IMetadataFileStructure> Files { get; set; } = new ObservableCollection<IMetadataFileStructure>();
        public IMetadataFileStructure SelectedFile { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Infos { get; } = new List<string>();
        public Action OnUnsetBusy { get; set; }

        public void Notify(string msg) { }
        public void LogDebug(string message) { }
        public void LogInfo(string message) => Infos.Add(message);
        public void LogError(string message) => Errors.Add(message);
        public void LogError(Exception ex, string message = null) => Errors.Add(message ?? ex.Message);
        public void SetBusyApp() { }
        public void UnsetBusyApp() => OnUnsetBusy?.Invoke();
        public void FilterFiles() { }
        public Func<string, bool> Filter { get; set; }
        public Func<string, bool> FilterPredicate() => Filter;
    }
}
