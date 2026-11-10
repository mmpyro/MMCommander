using Comander.CommanderIO;
using Comander.Converters;
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

        // Both panels share one queue so transfers run one after another ("Add to queue").
        public ProxyIO(IFileSystemManager fileManager, IStateMonitor monitor, IDialogService dialogs, JobQueue queue = null)
        {
            _fileManager = fileManager;
            _monitor = monitor;
            _dialogs = dialogs;
            _queue = queue ?? new JobQueue(monitor);
        }

        private readonly JobQueue _queue;

        public Task CopyFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir, ConflictPolicy policy = ConflictPolicy.Ask) =>
            Transfer(files, destinationDir, false, policy);

        public Task MoveFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir) =>
            Transfer(files, destinationDir, true, ConflictPolicy.Ask);

        // F5/F6: options dialog, then a queued job with progress; conflicts are asked from the worker thread.
        private async Task Transfer(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir, bool move, ConflictPolicy policy)
        {
            var items = files.Where(f => f.Name != "..").ToList();
            if (items.Count == 0)
            {
                _ioManager.LogError("Nothing selected.");
                return;
            }
            var space = _fileManager.GetSpace(destinationDir.FullName);
            var request = await _dialogs.AskCopy(new CopyDialogModel
            {
                IsMove = move,
                Names = items.Select(f => f.Name).ToList(),
                TotalSize = items.Where(f => !f.IsDirectory).Sum(f => f.Size),
                Destination = destinationDir.FullName,
                FreeSpaceText = space == null ? string.Empty : FileSizeConverter.FormatSpace(space.Free) + " free on " + space.Label,
                // Settings > File operations sets the default; Shift+F5 always keeps both.
                Policy = policy == ConflictPolicy.Ask ? AppSettings.Current.DefaultPolicy : policy,
                HasFilter = _ioManager.FilterPredicate() != null,
            });
            if (request == null)
                return;
            var destination = request.Destination;
            if (string.IsNullOrEmpty(destination) || !Directory.Exists(destination))
            {
                _ioManager.LogError(string.Format("Destination folder doesn't exist: {0}", destination));
                return;
            }
            var options = request.Options;
            if (request.OnlyMatchingFilter)
                options.Include = _ioManager.FilterPredicate();
            var verb = move ? "Moving" : "Copying";
            var paths = items.Select(f => f.FullName).ToList();
            var job = new TransferJob(verb, destination, j =>
            {
                Busy();
                try
                {
                    var engine = new TransferEngine(options, c => _dialogs.AskConflict(c).GetAwaiter().GetResult(), j, j.Pause, j.Token);
                    var result = move ? engine.Move(paths, destination) : engine.Copy(paths, destination);
                    foreach (var error in result.Errors)
                    {
                        j.AddError();
                        _ioManager.LogError(string.Format("Cannot {0} {1}", move ? "move" : "copy", error));
                    }
                    if (result.Cancelled)
                        _ioManager.LogInfo(verb + " cancelled.");
                    else
                        LogSummary(move ? "moved" : "copied", result.Copied + result.Errors.Count, result.Errors.Count);
                }
                finally
                {
                    Avaiable();
                }
            });
            var done = _queue.Enqueue(job);
            if (request.ShowProgress)
                _dialogs.ShowProgress(job);
            await done;
        }

        public Task CreateDirectory()
        {
            return CreateItem("New folder", path => _fileManager.CreateDirectory(path), "Create new directory ", "Directory wasn't created");
        }

        public Task CreateFile()
        {
            return CreateItem("New file", path => _fileManager.CreateFile(path), "Create new file ", "File wasn't created");
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

        // F8 moves to the Trash, Shift+F8 deletes permanently; the dialog can switch between both.
        public async Task DeleteFile(IEnumerable<IMetadataFileStructure> files, bool permanent = false)
        {
            var items = files.Where(f => f.Name != "..").ToList();
            if (items.Count == 0)
            {
                _ioManager.LogError("Nothing selected.");
                return;
            }
            var unavailable = items.Select(f => Trash.Unavailable(f.FullName)).FirstOrDefault(r => r != null);
            var request = await _dialogs.AskDelete(new DeleteDialogModel
            {
                Items = items.Select(f => new DeleteItem { Name = f.Name, SizeText = f.IsDirectory ? "—" : FileSizeConverter.Format(f.Size) }).ToList(),
                Folder = _ioManager.ActualPath,
                TotalSizeText = FileSizeConverter.Format(items.Where(f => !f.IsDirectory).Sum(f => f.Size)),
                Permanent = permanent,
                TrashUnavailable = unavailable,
            });
            if (request == null)
                return;
            var toTrash = !request.Permanent;
            var job = new TransferJob(toTrash ? "Moving to Trash" : "Deleting", toTrash ? "Trash" : _ioManager.ActualPath, j =>
            {
                Busy();
                try
                {
                    int failed = 0;
                    for (int i = 0; i < items.Count && !j.Token.IsCancellationRequested; i++)
                    {
                        var file = items[i];
                        j.Report(new TransferProgress { FileIndex = i + 1, FileCount = items.Count, CurrentName = file.Name });
                        try
                        {
                            if (toTrash) Trash.MoveToTrash(file.FullName); else file.Delete();
                            _ioManager.LogDebug((toTrash ? "Trash " : "Delete ") + file.Name);
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            j.AddError();
                            _ioManager.LogError(ex, string.Format("Cannot {0} {1}", toTrash ? "move to Trash" : "delete", file.Name));
                        }
                    }
                    LogSummary(toTrash ? "moved to Trash" : "deleted", items.Count, failed);
                }
                finally
                {
                    Avaiable();
                }
            });
            await _queue.Enqueue(job);
        }

        public async Task RenameFile(IMetadataFileStructure selectedFile)
        {
            var oldName = selectedFile.Name;
            var newName = await _dialogs.AskText("Rename", oldName);
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
                _ioManager.LogError(ex, string.Format("Cannot rename {0} to {1}", oldName, newName));
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
                    _ioManager.LogError(ex, "Zip failed");
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
                    _ioManager.LogError(ex, "Unzip failed");
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
