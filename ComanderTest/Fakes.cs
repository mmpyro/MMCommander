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
        public CopyOption OverrideAnswer { get; set; } = CopyOption.None;
        public int OverrideQuestions { get; private set; }

        public Task<bool> Confirm(string message) => Task.FromResult(true);
        public string TextAnswer { get; set; }
        public UnZipRequest UnzipAnswer { get; set; }

        public Task<string> AskText(string message, string initialText = "") => Task.FromResult(TextAnswer);
        public Task<CopyOption> AskOverride(string message)
        {
            OverrideQuestions++;
            return Task.FromResult(OverrideAnswer);
        }
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
    }
}
