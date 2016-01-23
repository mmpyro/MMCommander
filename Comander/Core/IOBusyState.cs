using Comander.CommanderIO;
using Comander.ViewModel;
using IOLib;
using System;
using System.Collections.Generic;

namespace Comander.Core
{
    public class IOBusyState : IOState
    {
        private IOManager _iOManager;

        public IOBusyState(IOManager _iOManager)
        {
            this._iOManager = _iOManager;
        }

        public void CopyFile(IEnumerable<IMetadataFileStructure> files, IAbstractFileStructure destinationDir)
        {
        }

        public void CreateDirectory()
        {
        }

        public void CreateFile()
        {
        }

        public void DeleteFile(IEnumerable<IMetadataFileStructure> files)
        {
        }

        public void Invoke()
        {
            _iOManager.SetBusyApp();
        }

        public void MoveFile(IEnumerable<IMetadataFileStructure> files, IAbstractFileStructure destinationDir)
        {
        }

        public void RenameFile(IMetadataFileStructure selectedFile)
        {
        }

        public void Run(IMetadataFileStructure selectedFile)
        {
        }

        public void RunAsAdmin(IMetadataFileStructure selectedFile)
        {
        }

        public void ShowPluginWindow(Action displayPluginWindow)
        {
        }

        public void UnZipFiles()
        {
        }

        public void ZipFiles()
        {
        }
    }
}
