using Comander.CommanderIO;
using Comander.Dtos;
using IOLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZipLib;

namespace Comander.Core
{
    public class ProxyIO : IProxyIO
    {
        private readonly IFileSystemManager _fileManager;
        private readonly IStateMonitor _monitor;
        private readonly IDialogService _dialogs;
        private IIOManager _ioManager;

        public IIOManager Manager
        {
            set
            {
                _ioManager = value;
            }
        }

        public ProxyIO(IFileSystemManager fileManager, IStateMonitor monitor, IDialogService dialogs)
        {
            _fileManager = fileManager;
            _monitor = monitor;
            _dialogs = dialogs;
        }

        public async Task CopyFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir)
        {
            var items = files.ToList();
            if (!Enter())
                return;
            // Remembers "override all" / "cancel" for the rest of this operation.
            var choice = CopyOption.None;
            Func<string, string, bool> allowOverride = (file, dir) =>
            {
                if (choice == CopyOption.OverrideAll)
                    return true;
                if (choice == CopyOption.Cancel)
                    return false;
                // Called on the worker thread; the dialog service marshals to the UI thread.
                choice = _dialogs.AskOverride(string.Format("File {0} exist in {1}.\nDo you want to override it ?", file, dir)).GetAwaiter().GetResult();
                return choice == CopyOption.OverrideAll || choice == CopyOption.OverrideSingle;
            };
            await RunBusy(() =>
            {
                int failed = 0;
                foreach (var file in items)
                {
                    try
                    {
                        _ioManager.Notify(string.Format("Copy {0} to {1}", file.Name, destinationDir.FullName));
                        file.Copy(destinationDir, allowOverride);
                        _ioManager.LogDebug(string.Format("Copy {0} to {1}", file.Name, destinationDir.FullName));
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _ioManager.LogError(ex, string.Format("Cannot copy {0} to {1}", file.Name, destinationDir.FullName));
                    }
                }
                LogSummary("copied", items.Count, failed);
            });
        }

        public Task CreateDirectory()
        {
            return CreateItem("Please enter the directory name", path => _fileManager.CreateDirectory(path), "Create new directory ", "Directory wasn't created");
        }

        public Task CreateFile()
        {
            return CreateItem("Please enter the file name", path => _fileManager.CreateFile(path), "Create new file ", "File wasn't created");
        }

        private async Task CreateItem(string question, Action<string> create, string debugMessage, string errorMessage)
        {
            var name = await _dialogs.AskText(question);
            if (string.IsNullOrEmpty(name) || !Enter())
                return;
            try
            {
                Busy();
                string path = Path.Combine(_ioManager.ActualPath, name);
                create(path);
                _ioManager.LogDebug(debugMessage + path);
            }
            catch
            {
                _ioManager.LogError(errorMessage);
            }
            finally
            {
                _monitor.Exit();
                Avaiable();
            }
        }

        public async Task DeleteFile(IEnumerable<IMetadataFileStructure> files)
        {
            var items = files.ToList();
            if (!await _dialogs.Confirm("Do you want delete " + items.Count + " files ?") || !Enter())
                return;
            await RunBusy(() =>
            {
                int iterator = 1, failed = 0;
                foreach (var file in items)
                {
                    try
                    {
                        _ioManager.Notify(string.Format("Delete {0}, {1} of {2}", file.Name, iterator++, items.Count));
                        file.Delete();
                        _ioManager.LogDebug("Delete " + file.Name);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _ioManager.LogError(ex, string.Format("Cannot delete {0} file", file.Name));
                    }
                }
                LogSummary("deleted", items.Count, failed);
            });
        }

        public async Task MoveFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir)
        {
            var items = files.ToList();
            if (!Enter())
                return;
            await RunBusy(() =>
            {
                int failed = 0;
                foreach (var file in items)
                {
                    try
                    {
                        _ioManager.Notify(string.Format("Move {0} to {1}", file.Name, destinationDir.FullName));
                        file.Move(destinationDir);
                        _ioManager.LogDebug(string.Format("Move {0} to {1}", file.Name, destinationDir.FullName));
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _ioManager.LogError(ex, string.Format("Cannot move {0} to {1}", file.Name, destinationDir.FullName));
                    }
                }
                LogSummary("moved", items.Count, failed);
            });
        }

        public async Task RenameFile(IMetadataFileStructure selectedFile)
        {
            var oldName = selectedFile.Name;
            var newName = await _dialogs.AskText("Please enter the new file name", oldName);
            if (string.IsNullOrEmpty(newName))
                return;
            // A bare name only: "../x" or "sub/x" would move the file (BUG-29).
            if (newName == "." || newName == ".." || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                _ioManager.LogError(string.Format("Invalid file name: {0}", newName));
                return;
            }
            try
            {
                selectedFile.Rename(newName);
                _ioManager.LogDebug(string.Format("Rename {0} to {1}", oldName, newName));
            }
            catch (Exception ex)
            {
                _ioManager.LogError(ex, string.Format("Cannot rename {0} to {1}: {2}", oldName, newName, ex.Message));
            }
            _ioManager.FilterFiles();
        }

        public void Run(IMetadataFileStructure selectedFile)
        {
            Shell.Open(selectedFile.FullName, _ioManager.ActualPath);
            _ioManager.LogDebug("Run " + selectedFile.FullName);
        }

        public void RunAsAdmin(IMetadataFileStructure selectedFile)
        {
            Shell.RunAsAdmin(selectedFile.FullName, _ioManager.ActualPath);
            _ioManager.LogDebug("Run as administrator " + selectedFile.FullName);
        }

        public async Task ZipFiles()
        {
            var files = _ioManager.Files.Where(t => t.IsSelected()).ToList();
            if (!files.Any())
            {
                _ioManager.LogError("Any file was selected.");
                return;
            }
            var zipParameters = await _dialogs.AskZip(_ioManager.ActualPath);
            if (zipParameters == null || !Enter())
                return;
            _ioManager.Notify("Zip in progress...");
            bool encrypt = !string.IsNullOrEmpty(zipParameters.Password);
            await RunBusy(() =>
            {
                try
                {
                    var zipper = new ZipAdapter();
                    if (files.Count == 1 && files[0].IsDirectory)
                    {
                        if (encrypt)
                            zipper.CompressDirWithEncryption(zipParameters, files[0].FullName);
                        else
                            zipper.CompressDir(zipParameters, files[0].FullName);
                        _ioManager.LogInfo(string.Format("Zip {0} was finished without errors.", files[0].Name));
                    }
                    else
                    {
                        var paths = files.Select(t => t.FullName).ToArray();
                        if (encrypt)
                            zipper.CompressFilesWithEncryption(zipParameters, paths);
                        else
                            zipper.CompressFiles(zipParameters, paths);
                        _ioManager.LogInfo("Zip proccess was finished without errors.");
                    }
                }
                catch (Exception ex)
                {
                    _ioManager.LogError(ex, "Error occur during zip. Check logs.");
                }
            });
        }

        public async Task UnZipFiles()
        {
            UnZipRequest request;
            try
            {
                request = await _dialogs.AskUnzip(_ioManager.SelectedFile);
            }
            catch (Exception e)
            {
                _ioManager.LogError(e.Message);
                return;
            }
            if (request == null || !Enter())
                return;
            var zipParameters = request.Parameters;
            await RunBusy(() =>
            {
                try
                {
                    var zipper = new ZipAdapter();
                    if (request.ExtractType == ExtractType.Here)
                        zipper.UnCompressFile(zipParameters, _ioManager.ActualPath);
                    else
                    {
                        string extractionPath = Path.Combine(_ioManager.ActualPath, Path.GetFileNameWithoutExtension(zipParameters.ArchiveName));
                        _fileManager.CreateDirectory(extractionPath);
                        zipper.UnCompressFile(zipParameters, extractionPath);
                    }
                    _ioManager.LogInfo(string.Format("UnZip {0} was finished without errors.", _ioManager.ActualPath));
                }
                catch (Exception ex)
                {
                    _ioManager.LogError(ex, "Error occur during unzip. Check logs.");
                }
            });
        }

        public async Task PasteFromClipboard(IMetadataFileStructure destinationDir)
        {
            var paths = await _dialogs.GetClipboardFiles();
            var fileFactory = new MetaDataFileFactory();
            var files = paths.Select(path => (IMetadataFileStructure)fileFactory.CreateFileMsg(path)).ToList();
            await CopyFile(files, destinationDir);
        }

        // One line per batch so a partial failure is visible (BUG-17).
        private void LogSummary(string verb, int total, int failed)
        {
            if (failed == 0 && total > 1)
                _ioManager.LogInfo(string.Format("{0} {1}.", total, verb));
            else if (failed > 0)
                _ioManager.LogError(string.Format("{0} {1}, {2} failed.", total - failed, verb, failed));
        }

        private bool Enter()
        {
            if (_monitor.Enter())
                return true;
            _ioManager.LogError("Another operation is in progress.");
            return false;
        }

        // Runs work on the thread pool; the busy lock is always released, even if work or the refresh throws (BUG-09).
        private Task RunBusy(Action work)
        {
            return Task.Run(() =>
            {
                try
                {
                    Busy();
                    work();
                }
                finally
                {
                    _monitor.Exit();
                    Avaiable();
                }
            });
        }

        private void Avaiable()
        {
            _ioManager.UnsetBusyApp();
        }

        private void Busy()
        {
            _ioManager.SetBusyApp();
        }
    }
}
