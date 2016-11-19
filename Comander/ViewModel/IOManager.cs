using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Comander.CommanderIO;
using Comander.View;
using IOLib;
using RxFramework;
using Search;
using Comander.Core;
using Comander.Messages;
using System.Collections.Specialized;

namespace Comander.ViewModel
{
    public partial class IOManager : ReactiveObject, IFileNotifier
    {
        #region Methods
        private void ZipFiles()
        {
            _proxy.ZipFiles();
        }

        private void UnZipFiles()
        {
            _proxy.UnZipFiles();
        }

        private void DeleteFile()
        {
           _proxy.DeleteFile(_files.Where(t => t.IsSelected()));
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

        private void SelectByType()
        {
            if (SelectedFile != null && !string.IsNullOrEmpty(SelectedFile.Ext))
            {
                Files.Where(t => t.Ext == SelectedFile.Ext).ToList().ForEach(t => t.PermanentSelect());
                RefreshFilesCollection();
            }
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
            _proxy.MoveFile(_files.Where(t => t.IsSelected()), SeccondManagerCurrentDir);
        }

        private async void Refresh()
        {
            var dirs = await _fileManager.GetAllStructuresAsync(ActualPath);
            Files = new ObservableCollection<IMetadataFileStructure>(dirs.Cast<IMetadataFileStructure>());
        }

        private void SetFocus()
        {
            if (_focus == true)
            {
                _messanger.Send(new SetFocusMessage
                {
                    GridName = _type.ToString(),
                    SelectedFile = GetFile()
                });
            }
        }

        private void SwitchFocus()
        {
            _focus = false;
            SecondManager._focus = true;
            SecondManager.SetFocus();
        }

        private IMetadataFileStructure GetFile()
        {
            if (SelectedFile != null)
                return SelectedFile;
            else if(Files.Any())
                return Files.First();
            return null;
        }

        public async void FilterFiles()
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
            _proxy.CopyFile(_files.Where(t => t.IsSelected()), SeccondManagerCurrentDir);
        }

        public void CreateDirectory()
        {
            _proxy.CreateDirectory();
        }

        public void CreateFile()
        {
            _proxy.CreateFile();
        }

        public void SetBusyApp()
        {
            _messanger.Send(new PulseMessage());
        }

        public void Notify(string msg)
        {
            CurrentOperation = msg;
        }

        public void UnsetBusyApp()
        {
            _messanger.Send(new WaitMessage());
            CurrentOperation = string.Empty;
            FilterFiles();
            _secondManager.FilterFiles();
            SetFocus();
        }

        private void RunAsAdmin()
        {
            _proxy.RunAsAdmin(SelectedFile);
        }

        private void Run()
        {
            _proxy.Run(SelectedFile);
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
            var processInfo = new ProcessStartInfo(_configReader["cmd"].Value);
            processInfo.WorkingDirectory = ActualPath;
            Process.Start(processInfo);
        }

        private void MouseMoveCallback(object e)
        {
            var args = e as WindowPositionEventArgs;
            if(args != null)
            {
                _currentPosition = args.CurrentCursorPosition;
            }
        }

        private void ShowInfoWindow()
        {
            InfoWindow infoWindow = new InfoWindow(SelectedFile);
            infoWindow.Left = _currentPosition.X;
            infoWindow.Top = _currentPosition.Y;
            infoWindow.Show();
        }

        private void RenameFile()
        {
            _proxy.RenameFile(SelectedFile);
        }

        private void EvaluatePath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _actualPath = _driveLetter;
            }
            else if (_pathResolver.ContainsPath(value))
            {
                string path = _pathResolver.GetPath(value);
                _actualPath = path;
                _historyManager.Add(path);
            }
            else
            {
                _actualPath = value;
                _historyManager.Add(value);
            }
        }

        public void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public void LogError(Exception ex)
        {
            if(ex is NullReferenceException)
            {
                Application.Current.Dispatcher.Invoke(() => _logger.Error("Any file was selected"));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => _logger.Error(ex));
            }
        }

        public void LogInfo(string message)
        {
            Application.Current.Dispatcher.Invoke(
                       () => _logger.Info(message));
        }

        public void ShowTreeWindow()
        {
            string path = SelectedFile.IsDirectory ? SelectedFile.FullName : ActualPath;
            TreeWindow window = new TreeWindow(path, this);
            window.Show();
        }

        public void PutSelectedFilesIntoClipboard()
        {
            StringCollection clipboardCollection = new StringCollection();
            foreach (var item in Files.Where(t => t.IsSelected()))
            {
                clipboardCollection.Add(item.FullName);
            }
            Clipboard.Clear();
            Clipboard.SetFileDropList(clipboardCollection);
        }

        public void TakeFilesFromClipboard()
        {
            _proxy.PasteFromClipboard(CurrentDir);
        }
        #endregion

        #region Property
        public IMetadataFileStructure CurrentDir
        {
            get
            {
                return (IMetadataFileStructure)_fileManager.GetDirFromPath(ActualPath);
            }
        }

        public IMetadataFileStructure SeccondManagerCurrentDir
        {
            get
            {
                return (IMetadataFileStructure)_fileManager.GetDirFromPath(_secondManager.ActualPath);
            }
        }

        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                OnPropertyChanged("Filter");
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
                SetFocus();
            }
        }

        public string ActualPath
        {
            get { return _actualPath; }
            set
            {
                EvaluatePath(value);
                CurrentOperation = string.Empty;
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

        public IProxyIO State
        {
            get
            {
                return _proxy;
            }
            set
            {
                _proxy = value;
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
           _proxy.ShowPluginWindow(() =>
           {
               var pluginWindow = new PluginWindow(_pluginManager, ActualPath, Files.Where(t => t.IsSelected()).Select(t => t.FullName), _logger);
               pluginWindow.Left = _currentPosition.X;
               pluginWindow.Top = _currentPosition.Y;
               pluginWindow.Show();
           });
        }

        public void LogError(string message)
        {
            _logger.Error(message);
        }
    }
}