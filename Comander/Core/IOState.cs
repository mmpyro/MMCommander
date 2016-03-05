using Comander.CommanderIO;
using IOLib;
using System;
using System.Collections.Generic;

namespace Comander.Core
{
    public interface IOState
    {
        void CopyFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir);
        void MoveFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir);
        void PasteFromClipboard(IMetadataFileStructure destinationDir);
        void ZipFiles();
        void UnZipFiles();
        void DeleteFile(IEnumerable<IMetadataFileStructure> files);
        void CreateFile();
        void CreateDirectory();
        void Run(IMetadataFileStructure selectedFile);
        void RunAsAdmin(IMetadataFileStructure selectedFile);
        void RenameFile(IMetadataFileStructure selectedFile);
        void ShowPluginWindow(Action displayPluginWindow);
        void Invoke();
    }

}
