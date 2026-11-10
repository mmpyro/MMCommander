using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Comander.CommanderIO;

namespace Comander.Core
{
    public interface IProxyIO
    {
        Task CopyFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir, IOLib.ConflictPolicy policy = IOLib.ConflictPolicy.Ask);
        Task MoveFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir);
        Task PasteFromClipboard(IMetadataFileStructure destinationDir);
        Task ZipFiles();
        Task UnZipFiles();
        Task DeleteFile(IEnumerable<IMetadataFileStructure> files, bool permanent = false);
        Task CreateFile();
        Task CreateDirectory();
        void Run(IMetadataFileStructure selectedFile);
        void RunAsAdmin(IMetadataFileStructure selectedFile);
        Task RenameFile(IMetadataFileStructure selectedFile);
        IIOManager Manager { set; }
    }

    // The part of a panel that ProxyIO uses. Notify, Log*, SetBusyApp and UnsetBusyApp may be called from any thread.
    public interface IIOManager
    {
        string ActualPath { get; }
        ObservableCollection<IMetadataFileStructure> Files { get; }
        IMetadataFileStructure SelectedFile { get; }
        void Notify(string msg);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogError(string message);
        void LogError(Exception ex, string message = null);
        void SetBusyApp();
        void UnsetBusyApp();
        void FilterFiles();
        // Null when the panel has no filter; otherwise true for paths the filter keeps.
        Func<string, bool> FilterPredicate();
    }
}
