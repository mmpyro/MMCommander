using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using Comander.CommanderIO;
using Comander.Core;
using Comander.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IOLib;
using LogLib;
using Comander.Messages;
using Search;
using System.Collections.Generic;
using System.Linq;

namespace Comander.ViewModel
{
    public class MainVM : ObservableObject
    {
        private IOManager _io1;
        private IOManager _io2;
        private readonly IConfigReader _configReader;
        private readonly IAssemblyVersionResolver _assemblyVersionResolver;
        private ObservableCollection<LogMsg> _logsCollection = new ObservableCollection<LogMsg>();
        private LogMsg _currentLogMsg;
        private readonly ILogger _logger;
        private int _current = 0;
        private string _logCount;
        private bool _isBusy;
        private IEnumerable<MenuItem> _exts;
        private IEnumerable<MenuItem> _programs;

        public MainVM(IOManager io1, IOManager io2, IConfigReader configReader, ILogger logger, IGenericCommandManager genericCommandManager ,IAssemblyVersionResolver assemblyVersionResolver)
        {
            _configReader = configReader;
            _logger = logger;
            _assemblyVersionResolver = assemblyVersionResolver;
            _logger.NotifyEvent += AddLogInfo;
            _io1 = io1;
            _io2 = io2;
            string keymapPath = Path.Combine(AppContext.BaseDirectory, "Manuals", "keymap.html");
            string syntaxPath = Path.Combine(AppContext.BaseDirectory, "Manuals", "syntax.html");

            SwitchCommand = new RelayCommand(SwitchCommanders);
            RefreshCommand = new RelayCommand(RefreshCommanders);
            SplitFileCommand = new RelayCommand(() => WindowHost.Show(() => new SplitWindow(_logger)));
            JoinFileCommand = new AsyncRelayCommand(JoinFile);
            ReadLogsCommand = new RelayCommand(() => Shell.Open(Path.Combine(Path.GetTempPath(), "mmcommander")));
            ClearLogCommand = new RelayCommand(ClearLog);
            NextLogCommand = new RelayCommand(NextLog);
            PreviusLogCommand = new RelayCommand(BackLog);
            CompareDirCommand = new RelayCommand(CompareDirectories);
            AboutCommand = new AsyncRelayCommand(() => WindowHost.ShowDialog(() => new AboutWindow(string.Format("Product version: {0}\nCreated by Michał Marszałek.",
                _assemblyVersionResolver.GetProductVersion(GetType())))));
            KeyMapCommand = new RelayCommand(() => Shell.Open(keymapPath));
            SyntaxCommand = new RelayCommand(() => Shell.Open(syntaxPath));
            DuplicateCommand = new RelayCommand(DuplicateCommanders);

            SearchCommand = new RelayCommand(() => WindowHost.Show(() => new SearchWindow(_io1, _logger)));
            var messanger = WeakReferenceMessenger.Default;
            messanger.Register<WindowCloseEventArgs>(this, (_, _) => OnClose());
            messanger.Register<PulseMessage>(this, (_, _) => Dispatcher.UIThread.Post(() => IsBusy = true));
            messanger.Register<WaitMessage>(this, (_, _) => Dispatcher.UIThread.Post(() => IsBusy = false));

            Exts = genericCommandManager.GetCommands().Select(t => new MenuItem
            {
                Header = t.Name,
                Command = t
            }).ToList();

            Programs = _configReader.GetPrograms().Select(t => new MenuItem
            {
                Header = t.Name,
                Command = new RelayCommand(() => Shell.Run(t))
            }).ToList();
        }

        private void CompareDirectories()
        {
            var fileMarker = new FileMarker(_io1.Files, _io2.Files, new FileParametersComparer());
            _io1.Files = new ObservableCollection<IMetadataFileStructure>(fileMarker.Mark());
        }

        // Loggers raise this from any thread; the collection is bound to the UI.
        private void AddLogInfo(LogInfo info, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogsCollection.Add(new LogMsg(info, message));
                NextLog();
            });
        }

        public void OnClose()
        {
            try
            {
                _configReader.SavePaths(_io1.ActualPath, _io2.ActualPath);
            }
            catch (Exception e)
            {
                _logger.Error("Cannot save settings", e);
            }
        }

        private void SwitchCommanders()
        {
            string bufforPath = _io1.ActualPath;
            _io1.ActualPath = _io2.ActualPath;
            _io2.ActualPath = bufforPath;
        }

        private void DuplicateCommanders()
        {
            _io2.ActualPath = _io1.ActualPath;
        }

        private void RefreshCommanders()
        {
            _io1.LoadDrivers();
            _io2.LoadDrivers();
        }

        private void JoinFileProcessor(string joinPath, string destinationPath, string joinedFileName)
        {
            Task.Run(() =>
            {
                try
                {
                    FileManager fileManager = new FileManager(new FileFactory());
                    Regex regex = new Regex(@"[0-9]*$");
                    string fileName = Path.GetFileName(joinPath);
                    fileName = regex.Replace(fileName, "");
                    fileManager.Join(Path.GetDirectoryName(joinPath), fileName, Path.Combine(destinationPath, joinedFileName));
                    _logger.Info("Join was finished without errors.");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            });
        }

        private async Task JoinFile()
        {
            var parameter = await WindowHost.ShowDialog<JoinFileDTO>(() => new JoinWindow());
            if (parameter != null)
            {
                _logger.Info("Join in progress...");
                JoinFileProcessor(parameter.JoinPath, parameter.DestinationPath, parameter.FileName);
            }
        }

        private void NextLog()
        {
            if (_current < _logsCollection.Count - 1)
                CurrentLogMsg = _logsCollection[++_current];
            else
                CurrentLogMsg = _logsCollection[_current];
            PerformLogCountMessage();
        }

        private void BackLog()
        {
            if (_current > 0)
            {
                CurrentLogMsg = _logsCollection[--_current];
                PerformLogCountMessage();
            }
        }

        private void PerformLogCountMessage()
        {
            LogCount = string.Format("{0} of {1}",_current+1, _logsCollection.Count);
        }

        private void ClearLog()
        {
            LogsCollection.Clear();
            CurrentLogMsg = null;
            LogCount = string.Empty;
            _current = 0;
        }


        #region Commands
        public ICommand SwitchCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand ClearLogCommand { get; set; }
        public ICommand SplitFileCommand { get; set; }
        public ICommand JoinFileCommand { get; set; }
        public ICommand NextLogCommand { get; set; }
        public ICommand PreviusLogCommand { get; set; }
        public ICommand CompareDirCommand { get; set; }
        public ICommand AboutCommand { get; set; }
        public ICommand KeyMapCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand ReadLogsCommand { get; set; }
        public ICommand SyntaxCommand { get; set; }
        public ICommand DuplicateCommand { get; set; }
        #endregion

        #region Property

        public IOManager Io1
        {
            get { return _io1; }
            set
            {
                _io1 = value;
                OnPropertyChanged();
            }
        }

        public IOManager Io2
        {
            get { return _io2; }
            set
            {
                _io2 = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LogMsg> LogsCollection
        {
            get { return _logsCollection; }
            set
            {
                _logsCollection = value;
                OnPropertyChanged();
            }
        }

        public LogMsg CurrentLogMsg
        {
            get { return _currentLogMsg; }
            set
            {
                if (Equals(value, _currentLogMsg)) return;
                _currentLogMsg = value;
                OnPropertyChanged();
            }
        }

        public string LogCount
        {
            get { return _logCount; }
            set
            {
                if (value == _logCount) return;
                _logCount = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        public IEnumerable<MenuItem> Exts
        {
            get
            {
                return _exts;
            }
            set
            {
                _exts = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<MenuItem> Programs
        {
            get
            {
                return _programs;
            }
            set
            {
                _programs = value;
                OnPropertyChanged();
            }
        }
        #endregion

    }
}