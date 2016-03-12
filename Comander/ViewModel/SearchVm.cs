using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Comander.Annotations;
using Comander.ViewModel;
using Comander.ViewModel.Commands;
using IOLib;
using LogLib;

namespace Search.ViewModel
{
    public class SearchVm : INotifyPropertyChanged
    {
        private Page _page;
        private SearchParameters _searchParameters;
        private ObservableCollection<IAbstractFileStructure> _files;
        private IAbstractFileStructure _selectedFile;
        private bool _isRunning;
        private Thread _task;
        private string _status = "";
        private object _searchOptions;
        private readonly ILogger _logger;

        public SearchVm(ILogger logger)
        {
            _logger = logger;
            try
            {
                Files = new ObservableCollection<IAbstractFileStructure>();
                NavigationCommand = new ExecuteCommand(() =>
                {
                    _task.Abort();
                    NavigationService navigationService = NavigationService.GetNavigationService(Page);
                    navigationService.Navigate(new Uri("../View/Pages/SettingsPage.xaml", UriKind.Relative));
                });
                StartCommand = new ExecuteCommand(StartSearch);
                StopCommand = new RelayCommand((t) => StopSearch(), Log, _ => true);
                SelectCommand = new ExecuteCommand(() =>
                {
                    var fileNotifier = Locator.Notifier;
                    if(fileNotifier != null && _selectedFile != null)
                        fileNotifier.Notify(_selectedFile);
                });
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        private void StopSearch()
        {
            _task.Abort();
            OnComplete();
        }

        private void StartSearch()
        {
            _task = new Thread(Search);
            _task.IsBackground = true;
            _task.Start();
            IsRunning = true;
        }

        private void Log(Exception ex)
        {
            Status = "[Error]: " + ex.Message;
            _logger.Error(ex);
        }

        private void Search()
        {
            try
            {
                _searchParameters = (SearchParameters)Application.Current.Properties["parameter"];
                _searchOptions = Application.Current.Properties["options"];
                ClearCollection();
                var directoryManager = new DirectoryManager(new FileFactory());
                directoryManager.OnFindFile += AddFileToList;
                if (_searchOptions is RegexOptions)
                {
                    var options = (RegexOptions)_searchOptions;
                    directoryManager.SearchFiles(_searchParameters, options);
                    OnComplete();
                }
                else if (_searchOptions is MatchOptions)
                {
                    var options = (MatchOptions)_searchOptions;
                    directoryManager.SearchFiles(_searchParameters, options);
                    OnComplete();
                }
                else
                    throw new ArgumentException("Invalid parameters");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private void OnComplete()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Status = string.Format("Finished: Files: {0}", Files.Count);
                IsRunning = !IsRunning;
            });
        }

        private void ClearCollection()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Files.Clear();
                Status = "";
            });
        }

        private void AddFileToList(IAbstractFileStructure file)
        {
            Application.Current.Dispatcher.Invoke(() => Files.Add(file));
        }

        #region Property
        public Page Page
        {
            get { return _page; }
            set
            {
                if (Equals(value, _page)) return;
                _page = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IAbstractFileStructure> Files
        {
            get { return _files; }
            set
            {
                _files = value;
                OnPropertyChanged();
            }
        }

        public IAbstractFileStructure SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                _selectedFile = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public ICommand NavigationCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SelectCommand { get; set; }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}