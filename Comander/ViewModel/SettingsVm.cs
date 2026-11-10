using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Comander.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOLib;

namespace Search.ViewModel
{
    public class SettingsVm : ObservableObject
    {
        private bool _recursive;
        private string _phrase;
        private string _rootPath;
        private bool _ignoreCase;
        private bool _wholeWord;
        private bool _contains;

        // search: shows the results page for these parameters (was Application.Current.Properties + page navigation).
        public SettingsVm(string rootPath, Action<SearchParameters> search)
        {
            RootPath = rootPath;
            Contains = true;
            NavigationCommand = new RelayCommand(() => search(CreateSearchParameters()), () => !string.IsNullOrEmpty(Phrase));
            SetDirectoryCommand = new AsyncRelayCommand(SetDirectory);
        }

        private SearchParameters CreateSearchParameters()
        {
            var searchParameters = new SearchParameters(RootPath, Phrase, Recursive);
            searchParameters.MatchOptions = WholeWord ? MatchOptions.WholeWord : MatchOptions.Contains;
            RegexOptions regexOptions = RegexOptions.None;
            if (_ignoreCase)
            {
                regexOptions = regexOptions | RegexOptions.IgnoreCase;
            }
            searchParameters.RegexOptions = regexOptions;
            return searchParameters;
        }

        private async Task SetDirectory()
        {
            RootPath = await WindowHost.PickFolder(RootPath) ?? RootPath;
        }

        public ICommand NavigationCommand { get; set; }
        public ICommand SetDirectoryCommand { get; set; }

        #region Property
        public bool WholeWord
        {
            get
            {
                return _wholeWord;
            }
            set
            {
                if (!(value == false && _contains == false))
                {
                    _wholeWord = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Contains
        {
            get
            {
                return _contains;
            }
            set
            {
                if (!(value == false && _wholeWord == false))
                {
                    _contains = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Recursive
        {
            get { return _recursive; }
            set
            {
                _recursive = value;
                OnPropertyChanged();
            }
        }

        public string Phrase
        {
            get { return _phrase; }
            set
            {
                _phrase = value;
                OnPropertyChanged();
                ((RelayCommand)NavigationCommand).NotifyCanExecuteChanged();
            }
        }

        public string RootPath
        {
            get { return _rootPath; }
            set
            {
                _rootPath = value;
                OnPropertyChanged();
            }
        }

        public bool IgnoreCase
        {
            get { return _ignoreCase; }
            set
            {
                _ignoreCase = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}