using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Comander.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOLib;
using LogLib;

namespace Search.ViewModel
{
    public class SearchVm : ObservableObject
    {
        private IAbstractFileStructure _selectedFile;
        private bool _isRunning;
        private string _status = "";
        private readonly ILogger _logger;
        private CancellationTokenSource _search;

        // back: returns to the settings page. Thread.Abort is gone (throws on .NET 10), so searches stop via a token (BUG-26).
        public SearchVm(ILogger logger, IFileNotifier fileNotifier, Action back)
        {
            _logger = logger;
            NavigationCommand = new RelayCommand(() =>
            {
                _search?.Cancel();
                back();
            });
            StartCommand = new AsyncRelayCommand(StartSearch);
            StopCommand = new RelayCommand(() => _search?.Cancel());
            SelectCommand = new RelayCommand(() =>
            {
                if (fileNotifier != null && _selectedFile != null)
                    fileNotifier.Notify(_selectedFile);
            });
        }

        public SearchParameters Parameters { get; set; }

        private async Task StartSearch()
        {
            if (Parameters == null)
                return;
            var search = _search = new CancellationTokenSource();
            Files.Clear();
            Status = "";
            IsRunning = true;
            try
            {
                var parameters = Parameters;
                await Task.Run(() =>
                {
                    var directoryManager = new DirectoryManager(new FileFactory());
                    directoryManager.OnFindFile += file =>
                    {
                        search.Token.ThrowIfCancellationRequested();
                        Dispatcher.UIThread.Post(() => Files.Add(file));
                    };
                    directoryManager.SearchFiles(parameters);
                }, search.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (System.IO.PathTooLongException)
            {
            }
            catch (Exception ex)
            {
                Status = "[Error]: " + ex.Message;
                _logger.Error(ex);
            }
            finally
            {
                // Set once, here only (BUG-14: it used to be toggled twice).
                IsRunning = false;
                Dispatcher.UIThread.Post(() => Status = string.Format("Finished: Files: {0}", Files.Count), DispatcherPriority.Background);
            }
        }

        #region Property
        public bool IsRunning
        {
            get { return _isRunning; }
            set { SetProperty(ref _isRunning, value); }
        }

        public ObservableCollection<IAbstractFileStructure> Files { get; } = new ObservableCollection<IAbstractFileStructure>();

        public IAbstractFileStructure SelectedFile
        {
            get { return _selectedFile; }
            set { SetProperty(ref _selectedFile, value); }
        }

        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }
        #endregion

        public ICommand NavigationCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SelectCommand { get; set; }
    }
}
