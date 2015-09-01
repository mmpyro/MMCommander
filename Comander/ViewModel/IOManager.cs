using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Comander.CommanderIO;
using Comander.Controls;
using Comander.View;
using IOLib;
using LogLib;
using RxFramework;
using Search;
using ZipLib;
using ZipAdapter = ZipLib.ZipAdapter;

namespace Comander.ViewModel
{
    public partial class IOManager : ReactiveObject, IFileNotifier
    {
        #region Methods
        //private bool AllowReplace(string fileName)
        //{
        //    bool result = false;
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        ConfirmWindow confirmWindow =
        //            new ConfirmWindow(string.Format("Do you want to replace {0} file ?", fileName));
        //        result = confirmWindow.ShowDialog() ?? false;
        //    });
        //    return result;
        //}

        private void ZipFileProcessor(IEnumerable<IMetadataFileStructure> zFiles, ZipParameters zipParameters)
        {
            Task.Run(() =>
            {
                try
                {
                    SetBusyApp();
                    ZipAdapter zipper = new ZipAdapter();
                    var files = (from file in zFiles select file.FullName).ToArray();
                    if(string.IsNullOrEmpty(zipParameters.Password))
                        zipper.CompressFiles(zipParameters, files);
                    else
                        zipper.CompressFilesWithEncryption(zipParameters, files);
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine("Zip proccess was finished without errors.", LogInfo.Info, LogLevel.Minimal));
                    UnsetBusyApp();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Error, LogLevel.Minimal));
                    UnsetBusyApp();
                }
                finally
                {
                    Notify(string.Empty);
                }
            });
        }

        private void ZipDirProcessor(IMetadataFileStructure dir, ZipParameters zipParameters)
        {
            Task.Run(() =>
            {
                try
                {
                    SetBusyApp();
                    ZipAdapter zipper = new ZipAdapter();
                    if (string.IsNullOrEmpty(zipParameters.Password))
                        zipper.CompressDir(zipParameters, dir.FullName);
                    else
                        zipper.CompressFilesWithEncryption(zipParameters, dir.FullName);
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine("Zip proccess was finished without errors.", LogInfo.Info, LogLevel.Minimal));
                    UnsetBusyApp();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Error, LogLevel.Minimal));
                    UnsetBusyApp();
                }
                finally
                {
                    Notify(string.Empty);
                }
            });
        }

        private void ZipFiles()
        {
            var files = Files.Where(t => t.IsSelected());
            if (files.Count() == 1 && files.First().IsDirectory)
            {
                ZipWindow zipWindow = new ZipWindow(_actualPath);
                zipWindow.ShowDialog();
                if (zipWindow.ZipParameters != null)
                {
                    Notify("Zip in progress...");
                    var parameters = zipWindow.ZipParameters;
                    ZipDirProcessor(files.First(), parameters);
                }
            }
            else if (files.Any())
            {
                ZipWindow zipWindow = new ZipWindow(_actualPath);
                zipWindow.ShowDialog();
                if ( zipWindow.ZipParameters != null)
                {
                    Notify("Zip in progress...");
                    var parameters = zipWindow.ZipParameters;
                    ZipFileProcessor( files, parameters);
                }
            }
            else
            {
                _logger.WriteLine("Any file was selected.", LogInfo.Warrning, LogLevel.Minimal);
            }
        }

        private void UnZipFiles()
        {
            try
            {
                UnZipWindow window = new UnZipWindow(SelectedFile);
                window.ShowDialog();
                if (window.ZipParameters != null)
                {
                    UnZipProcesor(window.ZipParameters, window.ExtractionType);
                }
            }
            catch (Exception e)
            {
                _logger.WriteLine(e.Message, LogInfo.Error, LogLevel.Minimal);
            }
        }

        private void UnZipProcesor(ZipParameters zipParameters, ExtractType extractionType)
        {
            Task.Run(() =>
            {
                try
                {
                    SetBusyApp();
                    var zipper = new ZipAdapter();
                    if (extractionType == ExtractType.Here)
                        zipper.UnCompressFile(zipParameters, ActualPath);
                    else
                    {
                        string dir = zipParameters.ArchiveName.Split('.')[0];
                        string extractionPath = Path.Combine(ActualPath, dir);
                        _fileManager.CreateDirectory(extractionPath);
                        zipper.UnCompressFile(zipParameters, extractionPath );
                    }
                    Application.Current.Dispatcher.Invoke(
                        () => _logger.WriteLine("UnZip proccess was finished without errors.", LogInfo.Info, LogLevel.Minimal));
                    UnsetBusyApp();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Error, LogLevel.Minimal));
                    UnsetBusyApp();
                }
                finally
                {
                    Notify(string.Empty);
                }
            });
        }

        private void DeleteFile()
        {
            var bfiles = _files.Where(t => t.IsSelected());
            int allfiles = bfiles.Count();
            ConfirmWindow confirmWindow = new ConfirmWindow("Do you want delete " + allfiles + " files ?");
            if (confirmWindow.ShowDialog() == true)
            {
                Task.Run(() =>
                {
                    try
                    {
                        SetBusyApp();
                        int iterator = 1;
                        foreach (var file in bfiles)
                        {
                            Notify(string.Format("Delete {0} of {1} files", iterator++, allfiles));
                            file.Delete();
                        }
                        UnsetBusyApp();
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Error, LogLevel.Minimal));
                        UnsetBusyApp();
                    }
                });
            }
        }

        public void LoadDrivers()
        {
            Drives = new ObservableCollection<DriveStruct>(_fileManager.GetDrives());
        }
        
        private void EnterIntoDir()
        {
            if (SelectedFile.IsDirectory)
                ActualPath = SelectedFile.FullName;
        }

        private void SelectAllFiles()
        {
            Files.ToList().ForEach(t => t.PermanentSelect());
            RefreshFilesCollection();
        }

        private void CountSelectedFiles()
        {
            CurrentOperation = "Selected elements: " + Files.Count(t => t.IsSelected());
        }

        private void ReversFileSelection()
        {
            Files.ToList().ForEach(t => t.SelectFile());
            RefreshFilesCollection();
        }

        private void RefreshFilesCollection()
        {
            Files = new ObservableCollection<IMetadataFileStructure>(Files);
            CountSelectedFiles();
        }

        private void MoveFile()
        {
            Task.Run(() =>
            {
                try
                {
                    SetBusyApp();
                    var bfiles = _files.Where(t => t.IsSelected());
                    int iterator = 1, allfiles = bfiles.Count();
                    IAbstractFileStructure destinationDir = _fileManager.GetDirFromPath(_secondManager.ActualPath);
                    foreach (var file in bfiles)
                    {
                        Notify(string.Format("Move {0} of {1} files",iterator++, allfiles));
                        file.Move(destinationDir);
                    }
                    UnsetBusyApp();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Error, LogLevel.Minimal));
                    UnsetBusyApp();
                }
            });
        }

        private async void Refresh()
        {
            var dirs = await _fileManager.GetAllStructuresAsync(ActualPath);
            Files = new ObservableCollection<IMetadataFileStructure>(dirs.Cast<IMetadataFileStructure>());
        }

        private async void FilterFiles()
        {
            try
            {
                if (string.IsNullOrEmpty(Filter))
                    Refresh();
                else
                {
                    var files = await _fileManager.GetFilesFromPathAsync(ActualPath);
                    var filterFiles = await _syntaxParser.PerformAsync(Filter, files.ToArray());
                    Files = new ObservableCollection<IMetadataFileStructure>(filterFiles.Cast<IMetadataFileStructure>());
                }
            }
            catch
            {
                Refresh();
            }
        }

        private void CopyFile()
        {
            Task.Run(() =>
            {
                try
                {
                    SetBusyApp();
                    var bfiles = _files.Where(t => t.IsSelected());
                    int iterator = 1, allfiles = bfiles.Count();
                    IAbstractFileStructure destinationDir = _fileManager.GetDirFromPath(_secondManager.ActualPath);
                    foreach (var file in bfiles)
                    {
                        Notify(string.Format("Copy {0} of {1} files", iterator++, allfiles));
                       file.OverrideCopy(destinationDir);
                    }
                    UnsetBusyApp();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Error, LogLevel.Minimal));
                    UnsetBusyApp();
                }
            });
        }

        public IAbstractFileStructure GetCurrentDirectory()
        {
            return _fileManager.GetDirFromPath(_actualPath);
        }

        public void CreateDirectory()
        {
            InputWindow inputWindow = new InputWindow("Please enter the directory name");
            if (inputWindow.ShowDialog() == true)
            {
                try
                {
                    string path = Path.Combine(ActualPath, inputWindow.InputName);
                    _fileManager.CreateDirectory(path);
                    FilterFiles();
                }
                catch
                {
                    _logger.WriteLine("Directory wasn't created", LogInfo.Error, LogLevel.Minimal);
                }
            }
        }

        public void CreateFile()
        {
            InputWindow inputWindow = new InputWindow("Please enter the file name");
            if (inputWindow.ShowDialog() == true)
            {
                try
                {
                    string path = Path.Combine(ActualPath, inputWindow.InputName);
                    _fileManager.CreateFile(path);
                    FilterFiles();
                }
                catch
                {
                    _logger.WriteLine("File wasn't created", LogInfo.Error, LogLevel.Minimal);
                }
            }
        }

        private void SetBusyApp()
        {
            IsAvaiable = false;
            Worker.Pulse();
        }

        private void Notify(string msg)
        {
            CurrentOperation = msg;
        }

        private void UnsetBusyApp()
        {
            Worker.Wait();
            CurrentOperation = string.Empty;
            IsAvaiable = true;
            FilterFiles();
            _secondManager.FilterFiles();
        }

        private void RunAsAdmin()
        {
            var info = new ProcessStartInfo
            {
                FileName = SelectedFile.FullName,
                WorkingDirectory = ActualPath,
                Verb = "runas"
            };
            Process.Start(info);
        }

        private void Run()
        {
            var info = new ProcessStartInfo
            {
                FileName = SelectedFile.FullName,
                WorkingDirectory = ActualPath,
            };
            Process.Start(info);
        }

        private void ShowShortcutsWindow()
        {
            var window = new ShortcutsWindow(_shortcutManager);
            window.ShowDialog();
            if (window.Selected != null)
            {
                ActualPath = window.Selected;
            }
        }

        private void ShowHistory()
        {
            var window = new HistoryWindow();
            window.Items = _historyManager.GetHistory();
            window.ShowDialog();
            if (window.Selected != null)
            {
                ActualPath = window.Selected;
            }
        }

        private void RunConsole()
        {
            var processInfo = new ProcessStartInfo(_configReader["cmd"]);
            processInfo.WorkingDirectory = ActualPath;
            Process.Start(processInfo);
        }

        private void ShowInfoWindow()
        {
            InfoWindow infoWindow = new InfoWindow(SelectedFile);
            var point = _mainWindowEventResolver.MousePoint;
            var positionPoint = _mainWindowEventResolver.GetWindowsPositionAction();
            infoWindow.Left = positionPoint.X + point.X;
            infoWindow.Top = positionPoint.Y + point.Y;
            infoWindow.Show();
        }

        private void RenameFile()
        {
            var inputWindow = new InputWindow("Please enter the new file name", SelectedFile.Name);
            if (inputWindow.ShowDialog() == true)
            {
                SelectedFile.Rename(inputWindow.InputName);
                FilterFiles();
            }
        }
        #endregion

        #region Property

        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                OnPropertyChanged("Filter");
            }
        }

        public ObservableCollection<IMetadataFileStructure> SelectedFiles
        {
            get { return _selectedFiles; }
            set
            {
                _selectedFiles = value;
                OnPropertyChanged("SelectedFiles");
            }
        }

        public bool IsAvaiable
        {
            get { return _isAvaiable; }
            set
            {
                _isAvaiable = value;
                OnPropertyChanged("IsAvaiable");
            }
        }

        public string CurrentOperation
        {
            get { return _currentOperation; }
            set
            {
                _currentOperation = value;
                OnPropertyChanged("CurrentOperation");
            }
        }

        public IMetadataFileStructure RootDirectory
        {
            get { return _rootDirectory; }
            set
            {
                _rootDirectory = value;
            }
        }

        public IOManager SecondManager
        {
            get { return _secondManager; }
            set
            {
                _secondManager = value;
                OnPropertyChanged("SecondManager");
            }
        }

        public IMetadataFileStructure SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged("SelectedFile");
            }
        }

        public ObservableCollection<IMetadataFileStructure> Files
        {
            get { return _files; }
            set
            {
                _files = value;
                OnPropertyChanged("Files");
            }
        }

        public string ActualPath
        {
            get { return _actualPath; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _actualPath = _driveLetter;
                    CurrentOperation = string.Empty;
                }
                else
                {
                    _actualPath = value;
                    _historyManager.Add(value);
                    CurrentOperation = string.Empty;
                }
                FilterFiles();
                OnPropertyChanged("ActualPath");
            }
        }

        public string DriveLetter
        {
            get { return _driveLetter; }
            set
            {
                if (value != null)
                {
                    _driveLetter = value;
                    _rootDirectory = null;
                    ActualPath = _driveLetter;
                    OnPropertyChanged("DriveLetter");
                }
            }
        }

        public ObservableCollection<DriveStruct> Drives
        {
            get { return _drives; }
            set
            {
                _drives = value;
                OnPropertyChanged("Drives");
            }
        }

        #endregion

        public void Notify(IAbstractFileStructure file)
        {
            if (file != null)
            {
                if (file.IsDirectory)
                    ActualPath = file.FullName;
                else
                {
                    string path = file.FullName.Substring(0, file.FullName.Length - file.Name.Length);
                    ActualPath = path;
                }
            }
        }

        private void ShowPluginWindow()
        {
            var pluginWindow = new PluginWindow(_pluginManager, Files.Where(t => t.IsSelected()), _logger);
            var point = _mainWindowEventResolver.MousePoint;
            var positionPoint = _mainWindowEventResolver.GetWindowsPositionAction();
            pluginWindow.Left = positionPoint.X + point.X;
            pluginWindow.Top = positionPoint.Y + point.Y;
            pluginWindow.Show();
        }
    }
}