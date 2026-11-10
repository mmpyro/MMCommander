using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Comander.CommanderIO;
using Comander.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using IOLib;
using Comander.Core;
using Comander.Messages;
using Search;

namespace Comander.ViewModel
{
    public partial class IOManager : ObservableObject, IFileNotifier, IIOManager
    {
        #region Methods
        private List<IMetadataFileStructure> SelectedFiles()
        {
            return _files.Where(t => t.IsSelected()).ToList();
        }

        public void LoadDrivers()
        {
            Drives = new ObservableCollection<DriveStruct>(_fileManager.GetDrives());
        }

        private void EnterIntoDir()
        {
            if (SelectedFile != null && SelectedFile.IsDirectory)
                ActualPath = SelectedFile.FullName;
        }

        private async Task ChooseDirectory()
        {
            var path = await WindowHost.PickFolder(ActualPath);
            if (path != null)
                ActualPath = path;
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

        private void Refresh()
        {
            FilterFiles();
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

        public void FilterFiles()
        {
            Dispatcher.UIThread.Post(() => _ = LoadFiles(TimeSpan.Zero));
        }

        // Lists ActualPath, applies Filter, and drops the result if a newer call started meanwhile (BUG-11).
        private async Task LoadFiles(TimeSpan delay)
        {
            _listing.Cancel();
            var listing = _listing = new CancellationTokenSource();
            var path = ActualPath;
            var filter = Filter;
            try
            {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, listing.Token);
                // The path box updates ActualPath on every keystroke; ignore paths that don't exist (yet).
                if (!System.IO.Directory.Exists(path))
                    return;
                var files = await _fileManager.GetAllStructuresAsync(path);
                if (!string.IsNullOrEmpty(filter))
                {
                    try
                    {
                        files = await _syntaxParser.PerformAsync(filter, files.ToArray());
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Invalid filter: " + ex.Message);
                    }
                }
                if (!listing.IsCancellationRequested)
                    Files = new ObservableCollection<IMetadataFileStructure>(files.Cast<IMetadataFileStructure>());
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                // e.g. access denied: say so instead of showing an empty folder (BUG-13).
                _logger.Error(string.Format("Cannot open {0}: {1}", path, ex.Message));
            }
        }

        public void SetBusyApp()
        {
            _messanger.Send(new PulseMessage());
        }

        public void Notify(string msg)
        {
            Dispatcher.UIThread.Post(() => CurrentOperation = msg);
        }

        public void UnsetBusyApp()
        {
            _messanger.Send(new WaitMessage());
            Dispatcher.UIThread.Post(() =>
            {
                CurrentOperation = string.Empty;
                FilterFiles();
                _secondManager.FilterFiles();
                SetFocus();
            });
        }

        private async Task ShowShortcutsWindow()
        {
            var selected = await WindowHost.ShowDialog<string>(() => new ShortcutsWindow(_shortcutManager));
            if (selected != null)
                ActualPath = selected;
        }

        private async Task ShowHistory()
        {
            var selected = await WindowHost.ShowDialog<string>(() => new HistoryWindow(_historyManager.GetHistory()));
            if (selected != null)
                ActualPath = selected;
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

        // The logger raises GUI events; MainVM marshals them to the UI thread, so these are safe from any thread.
        public void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public void LogError(Exception ex, string message = null)
        {
            if(ex is NullReferenceException)
                _logger.Error("Any file was selected");
            else if (string.IsNullOrEmpty(message))
                _logger.Error(ex);
            else
                _logger.Error(message, ex);
        }

        public void LogInfo(string message)
        {
            _logger.Info(message);
        }

        public void LogError(string message)
        {
            _logger.Error(message);
        }

        public void ShowTreeWindow()
        {
            string path = SelectedFile != null && SelectedFile.IsDirectory ? SelectedFile.FullName : ActualPath;
            WindowHost.Show(() => new TreeWindow(path, this));
        }

        private void ShowPluginWindow()
        {
            var files = SelectedFiles().Select(t => t.FullName).ToList();
            WindowHost.Show(() => new PluginWindow(_pluginManager, ActualPath, files, _logger));
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

        // Typing in the filter box waits 1 s for more input before listing (was Rx Throttle).
        public string Filter
        {
            get { return _filter; }
            set
            {
                if (SetProperty(ref _filter, value))
                    _ = LoadFiles(string.IsNullOrEmpty(value) ? TimeSpan.Zero : TimeSpan.FromSeconds(1));
            }
        }

        public string CurrentOperation
        {
            get { return _currentOperation; }
            set
            {
                _currentOperation = value;
                OnPropertyChanged();
            }
        }

        public IOManager SecondManager
        {
            get { return _secondManager; }
            set
            {
                _secondManager = value;
                OnPropertyChanged();
            }
        }

        public IMetadataFileStructure SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IMetadataFileStructure> Files
        {
            get { return _files; }
            set
            {
                _files = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                    ActualPath = _driveLetter;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<DriveStruct> Drives
        {
            get { return _drives; }
            set
            {
                _drives = value;
                OnPropertyChanged();
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
                    ActualPath = System.IO.Path.GetDirectoryName(file.FullName);
            }
        }
    }
}
