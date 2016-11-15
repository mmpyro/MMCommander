using Comander.CommanderIO;
using Comander.View;
using Comander.ViewModel;
using IOLib;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ZipLib;


namespace Comander.Core
{
    public class ProxyIO : IProxyIO
    {
        private readonly IFileSystemManager _fileManager;
        private IOManager _ioManager;
        private readonly IStateMonitor _monitor;

        public IOManager Manager
        {
            set
            {
                _ioManager = value;
            }
        }

        public ProxyIO( IFileSystemManager fileManager, IStateMonitor monitor)
        {
            _fileManager = fileManager;
            _monitor = monitor;
        }

        public void CopyFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir)
        {
            if(_monitor.Enter())
                Task.Run(() =>
                {
                    try
                    {
                        Busy();
                        int iterator = 1, allfiles = files.Count();
                        foreach (var file in files)
                        {
                            _ioManager.Notify(string.Format("Copy {0} of {1} files", iterator++, allfiles));
                            file.OverrideCopy(destinationDir);
                            _ioManager.LogDebug(string.Format("Copy {0} to {1}", file.Name, destinationDir.FullName));
                        }
                    }
                    catch (Exception ex)
                    {
                        _ioManager.LogError(ex);
                    }
                    finally
                    {
                        Avaiable();
                    }
                }).ContinueWith(t => _monitor.Exit(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void CreateDirectory()
        {
            if (_monitor.Enter())
            {
                InputWindow inputWindow = new InputWindow("Please enter the directory name");
                if (inputWindow.ShowDialog() == true)
                {
                    try
                    {
                        Busy();
                        string path = Path.Combine(_ioManager.ActualPath, inputWindow.InputName);
                        _fileManager.CreateDirectory(path);
                        _ioManager.LogDebug("Create new directory " + path);
                        _ioManager.FilterFiles();
                    }
                    catch
                    {
                        _ioManager.LogError("Directory wasn't created");
                    }
                    finally
                    {
                        Avaiable();
                        _monitor.Exit();
                    }
                }
                else
                {
                    _monitor.Exit();
                }
            }
        }

        public void CreateFile()
        {
            if (_monitor.Enter())
            {
                InputWindow inputWindow = new InputWindow("Please enter the file name");
                if (inputWindow.ShowDialog() == true)
                {
                    try
                    {
                        Busy();
                        string path = Path.Combine(_ioManager.ActualPath, inputWindow.InputName);
                        _fileManager.CreateFile(path);
                        _ioManager.LogDebug("Create new file " + path);
                        _ioManager.FilterFiles();
                    }
                    catch
                    {
                        _ioManager.LogError("File wasn't created");
                    }
                    finally
                    {
                        Avaiable();
                        _monitor.Exit();
                    }
                }
                else
                {
                    _monitor.Exit();
                }
            }
        }

        public void DeleteFile(IEnumerable<IMetadataFileStructure> files)
        {
            if (_monitor.Enter())
            {
                int allfiles = files.Count();
                ConfirmWindow confirmWindow = new ConfirmWindow("Do you want delete " + allfiles + " files ?");
                if (confirmWindow.ShowDialog() == true)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Busy();
                            int iterator = 1;
                            foreach (var file in files)
                            {
                                _ioManager.Notify(string.Format("Delete {0} of {1} files", iterator++, allfiles));
                                file.Delete();
                                _ioManager.LogDebug("Delete " + file.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _ioManager.LogError(ex);
                        }
                        finally
                        {
                            Avaiable();
                        }
                    }).ContinueWith(t => _monitor.Exit(), TaskContinuationOptions.OnlyOnRanToCompletion); ;
                }
                else
                {
                    _monitor.Exit();
                }
            }
        }

        public void MoveFile(IEnumerable<IMetadataFileStructure> files, IMetadataFileStructure destinationDir)
        {
            if(_monitor.Enter())
                Task.Run(() =>
                {
                    try
                    {
                        Busy();

                        int iterator = 1, allfiles = files.Count();
                        foreach (var file in files)
                        {
                            _ioManager.Notify(string.Format("Move {0} of {1} files", iterator++, allfiles));
                            file.Move(destinationDir);
                            _ioManager.LogDebug(string.Format("Move {0} to {1}", file.Name, destinationDir.FullName));
                        }
                    }
                    catch (Exception ex)
                    {
                        _ioManager.LogError(ex);
                    }
                    finally
                    {
                        Avaiable();
                    }
                }).ContinueWith(t => _monitor.Exit(),TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void RenameFile(IMetadataFileStructure selectedFile)
        {
            var inputWindow = new InputWindow("Please enter the new file name", selectedFile.Name);
            if (inputWindow.ShowDialog() == true)
            {
                selectedFile.Rename(inputWindow.InputName);
                _ioManager.LogDebug(string.Format("Rename {0} to {1}", selectedFile.Name, inputWindow.InputName));
                _ioManager.FilterFiles();
            }
        }

        public void Run(IMetadataFileStructure selectedFile)
        {
            var info = new ProcessStartInfo
            {
                FileName = selectedFile.FullName,
                WorkingDirectory = _ioManager.ActualPath,
            };
            Process.Start(info);
            _ioManager.LogDebug("Run " + selectedFile.FullName);
        }

        public void RunAsAdmin(IMetadataFileStructure selectedFile)
        {
            var info = new ProcessStartInfo
            {
                FileName = selectedFile.FullName,
                WorkingDirectory = _ioManager.ActualPath,
                Verb = "runas"
            };
            Process.Start(info);
            _ioManager.LogDebug("Run as administrator " + selectedFile.FullName);
        }

        public void ShowPluginWindow(Action displayPluginWindow)
        {
            displayPluginWindow();
        }

        public void ZipFiles()
        {
            var files = _ioManager.Files.Where(t => t.IsSelected());
            if (files.Count() == 1 && files.First().IsDirectory)
            {
                ZipWindow zipWindow = new ZipWindow(_ioManager.ActualPath);
                zipWindow.ShowDialog();
                if (zipWindow.ZipParameters != null)
                {
                    _ioManager.Notify("Zip in progress...");
                    var parameters = zipWindow.ZipParameters;
                    ZipDirProcessor(files.First(), parameters);
                }
            }
            else if (files.Any())
            {
                ZipWindow zipWindow = new ZipWindow(_ioManager.ActualPath);
                zipWindow.ShowDialog();
                if (zipWindow.ZipParameters != null)
                {
                    _ioManager.Notify("Zip in progress...");
                    var parameters = zipWindow.ZipParameters;
                    ZipFileProcessor(files, parameters);
                }
            }
            else
            {
                _ioManager.LogError("Any file was selected.");
            }
        }

        public void UnZipFiles()
        {
            try
            {
                UnZipWindow window = new UnZipWindow(_ioManager.SelectedFile);
                window.ShowDialog();
                if (window.ZipParameters != null)
                {
                    UnZipFilesProcessor(window.ZipParameters, window.ExtractionType);
                }
            }
            catch (Exception e)
            {
                _ioManager.LogError(e.Message);
            }
        }

        private void ZipFileProcessor(IEnumerable<IMetadataFileStructure> zFiles, ZipParameters zipParameters)
        {
            if(_monitor.Enter())
                Task.Run(() =>
                {
                    try
                    {
                        Busy();
                        ZipAdapter zipper = new ZipAdapter();
                        var files = (from file in zFiles select file.FullName).ToArray();
                        if (string.IsNullOrEmpty(zipParameters.Password))
                            zipper.CompressFiles(zipParameters, files);
                        else
                            zipper.CompressFilesWithEncryption(zipParameters, files);
                        _ioManager.LogInfo("Zip proccess was finished without errors.");
                    }
                    catch (Exception ex)
                    {
                        _ioManager.LogError(ex);
                    }
                    finally
                    {
                        _ioManager.Notify(string.Empty);
                        Avaiable();
                    }
                }).ContinueWith(t => _monitor.Exit(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void ZipDirProcessor(IMetadataFileStructure dir, ZipParameters zipParameters)
        {
            if(_monitor.Enter())
                Task.Run(() =>
                {
                    try
                    {
                        Busy();
                        ZipAdapter zipper = new ZipAdapter();
                        if (string.IsNullOrEmpty(zipParameters.Password))
                            zipper.CompressDir(zipParameters, dir.FullName);
                        else
                            zipper.CompressFilesWithEncryption(zipParameters, dir.FullName);
                        _ioManager.LogInfo(string.Format("Zip {0} was finished without errors.", dir.Name));
                    }
                    catch (Exception ex)
                    {
                        _ioManager.LogError(ex);
                    }
                    finally
                    {
                        _ioManager.Notify(string.Empty);
                        Avaiable();
                    }
                }).ContinueWith(t => _monitor.Exit(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void UnZipFilesProcessor(ZipParameters zipParameters, ExtractType extractionType)
        {
            if(_monitor.Enter())
                Task.Run(() =>
                {
                    try
                    {
                        Busy();
                        var zipper = new ZipAdapter();
                        if (extractionType == ExtractType.Here)
                            zipper.UnCompressFile(zipParameters, _ioManager.ActualPath);
                        else
                        {
                            string dir = zipParameters.ArchiveName.Split('.')[0];
                            string extractionPath = Path.Combine(_ioManager.ActualPath, dir);
                            _fileManager.CreateDirectory(extractionPath);
                            zipper.UnCompressFile(zipParameters, extractionPath);
                        }
                        _ioManager.LogInfo(string.Format("UnZip {0} was finished without errors.", _ioManager.ActualPath));
                    }
                    catch (Exception ex)
                    {
                        _ioManager.LogError(ex);
                    }
                    finally
                    {
                        Avaiable();
                        _ioManager.Notify(string.Empty);
                    }
                }).ContinueWith(t => _monitor.Exit(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void Avaiable()
        {
            _ioManager.UnsetBusyApp();
        }

        private void Busy()
        {
            _ioManager.SetBusyApp();
        }

        public void PasteFromClipboard(IMetadataFileStructure destinationDir)
        {
            StringCollection paths = Clipboard.GetFileDropList();
            MetaDataFileFactory fileFactory = new MetaDataFileFactory();
            List<IMetadataFileStructure> files = new List<IMetadataFileStructure>();
            foreach (var path in paths)
            {
                files.Add((IMetadataFileStructure)fileFactory.CreateFileMsg(path));
            }
            CopyFile(files, destinationDir);
        }
    }
}
