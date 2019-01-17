using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;
using Comander.Annotations;
using Comander.ViewModel.Commands;
using IOLib;
using System.Linq;
using Application = System.Windows.Application;
using Messanger;

namespace Search.ViewModel
{
    public class SettingsVm : INotifyPropertyChanged
    {
        private Page _page;
        private bool _recursive;
        private string _phrase;
        private string _rootPath;
        private bool _ignoreCase;
        private bool _wholeWord;
        private bool _contains;

        public SettingsVm()
        {
            RootPath = @"C:\";
            Contains = true;

            NavigationCommand = new ExecuteCommand(() =>
            {
                NavigationService navigationService = NavigationService.GetNavigationService(Page);
                Application.Current.Properties["parameter"] = CreateSearchParameters();

                navigationService.Navigate(new Uri("../View/Pages/SearchPage.xaml", UriKind.Relative));
            });
            SetDirectoryCommand = new ExecuteCommand(SetDirectory);
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

        private void SetDirectory()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = RootPath;
            DialogResult result = dialog.ShowDialog();
            if (result.Equals(DialogResult.OK))
            {
                RootPath = dialog.SelectedPath;
            }
        }

        public ICommand NavigationCommand { get; set; }
        public ICommand SetDirectoryCommand { get; set; }

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