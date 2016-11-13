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
using Application = System.Windows.Application;

namespace Search.ViewModel
{
    public class SettingsVm : INotifyPropertyChanged
    {
        private Page _page;
        private bool _recursive;
        private string _fraze;
        private string _rootPath;
        private string _searchType;
        private string _optionType;
        private bool _cultureInvariant;
        private bool _ignoreCase;
        private bool _rigthToLeft;
        private bool _ignorePatternWhiteSpace;


        public SettingsVm()
        {
            RootPath = @"C:\";
            _searchType = "Text";
            _optionType = "Whole";
            NavigationCommand = new ExecuteCommand(() =>
            {
                NavigationService navigationService = NavigationService.GetNavigationService(Page);
                Application.Current.Properties["parameter"] = CreateSearchParameters();
                Application.Current.Properties["options"] = CreateSearchOptions();
                navigationService.Navigate(new Uri("../View/Pages/SearchPage.xaml", UriKind.Relative));
            });
            TypeCommand = new RelayCommand( t => _searchType = t.ToString());
            OptionCommand = new RelayCommand(t => _optionType = t.ToString());
            SetDirectoryCommand = new ExecuteCommand(SetDirectory);
        }

        private object CreateSearchOptions()
        {
            if (_searchType.Equals("Text"))
            {
                return _optionType.Equals("Whole") ? MatchOptions.WholeWord : MatchOptions.Contains;
            }
            else
            {
                RegexOptions options = RegexOptions.None;
                if (_ignoreCase)
                    options = options | RegexOptions.IgnoreCase;
                if (_cultureInvariant)
                    options = options | RegexOptions.CultureInvariant;
                if (_rigthToLeft)
                    options = options | RegexOptions.RightToLeft;
                if (_ignorePatternWhiteSpace)
                    options = options | RegexOptions.IgnorePatternWhitespace;
                return options;
            }
        }

        private object CreateSearchParameters()
        {
            return new SearchParameters(RootPath, Fraze, Recursive);
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
        public ICommand OptionCommand { get; set; }
        public ICommand TypeCommand { get; set; }
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

        public bool Recursive
        {
            get { return _recursive; }
            set
            {
                _recursive = value;
                OnPropertyChanged();
            }
        }

        public string Fraze
        {
            get { return _fraze; }
            set
            {
                _fraze = value;
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

        public bool IgnorePatternWhiteSpace
        {
            get { return _ignorePatternWhiteSpace; }
            set
            {
                _ignorePatternWhiteSpace = value;
                OnPropertyChanged();
            }
        }

        public bool RigthToLeft
        {
            get { return _rigthToLeft; }
            set
            {
                _rigthToLeft = value;
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

        public bool CultureInvariant
        {
            get { return _cultureInvariant; }
            set
            {
                _cultureInvariant = value;
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