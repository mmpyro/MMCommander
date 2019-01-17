using Comander.CommanderIO;
using Comander.ViewModel;
using System;
using System.Collections.Generic;

namespace Comander.Core
{
    public interface IProxyIO
    {
        void CopyFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir, Func<string, string, bool> allowOverride);
        void MoveFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir);
        void PasteFromClipboard(IMetadataFileStructure destinationDir, Func<string, string, bool> allowOverride);
        void ZipFiles();
        void UnZipFiles();
        void DeleteFile(IEnumerable<IMetadataFileStructure> files);
        void CreateFile();
        void CreateDirectory();
        void Run(IMetadataFileStructure selectedFile);
        void RunAsAdmin(IMetadataFileStructure selectedFile);
        void RenameFile(IMetadataFileStructure selectedFile);
        void ShowPluginWindow(Action displayPluginWindow);
        IOManager Manager { set; }
    }

}
