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
        private int _errorCount;
        private readonly IDialogService _dialogs;
        private IOManager _activePanel;
        private bool _isBusy;
        private IEnumerable<MenuItem> _exts;
        private IEnumerable<MenuItem> _programs;

        public MainVM(IOManager io1, IOManager io2, IConfigReader configReader, ILogger logger, IGenericCommandManager genericCommandManager ,IAssemblyVersionResolver assemblyVersionResolver,
            JobQueue jobs = null, IDialogService dialogs = null)
        {
            Jobs = jobs ?? new JobQueue(new StateMonitor());
            _dialogs = dialogs;
            // The status bar job chip reopens the progress window of a job running in the background.
            ShowJobCommand = new RelayCommand(() => { if (Jobs.Current != null) dialogs?.ShowProgress(Jobs.Current); });
            _configReader = configReader;
            _logger = logger;
            _assemblyVersionResolver = assemblyVersionResolver;
            _logger.NotifyEvent += AddLogInfo;
            _io1 = io1;
            _io2 = io2;
            KeyMap = new KeyMap(configReader.KeyBindings);
            Palette = new PaletteVM(this);
            string syntaxPath = Path.Combine(AppContext.BaseDirectory, "Manuals", "syntax.html");

            SwitchCommand = new RelayCommand(SwitchCommanders);
            RefreshCommand = new RelayCommand(RefreshCommanders);
            SplitFileCommand = new RelayCommand(() => WindowHost.Show(() => new SplitWindow(_logger)));
            JoinFileCommand = new AsyncRelayCommand(JoinFile);
            ReadLogsCommand = new RelayCommand(() => Shell.Open(Path.Combine(Path.GetTempPath(), "mmcommander")));
            ClearLogCommand = new RelayCommand(ClearLog);
            CompareDirCommand = new RelayCommand(() => CompareDirectories(false));
            SettingsCommand = new AsyncRelayCommand(() => ShowSettings(0));
            SyncDirsCommand = new RelayCommand(() => CompareDirectories(true));
            AboutCommand = new AsyncRelayCommand(() => WindowHost.ShowDialog(() => new AboutWindow(_assemblyVersionResolver.GetProductVersion(GetType()))));
            KeyMapCommand = new AsyncRelayCommand(() => ShowSettings(1)); // opens Settings on the Keyboard page
            SyntaxCommand = new RelayCommand(() => Shell.Open(syntaxPath));
            DuplicateCommand = new RelayCommand(DuplicateCommanders);

            SearchCommand = new RelayCommand(() => WindowHost.Show(() => new SearchWindow(_io1, _logger)));
            var messanger = WeakReferenceMessenger.Default;
            messanger.Register<WindowCloseEventArgs>(this, (_, _) => OnClose());
            messanger.Register<PulseMessage>(this, (_, _) => Dispatcher.UIThread.Post(() => IsBusy = true));
            messanger.Register<WaitMessage>(this, (_, _) => Dispatcher.UIThread.Post(() => IsBusy = false));
            // Toolbar, menus and the F-key bar act on the panel that last had focus.
            messanger.Register<FocusMessage>(this, (_, msg) =>
                ActivePanel = msg.ManagerName.Equals(ManagerType.Io2.ToString(), StringComparison.OrdinalIgnoreCase) ? _io2 : _io1);
            ActivePanel = _io1;
            _io1.PropertyChanged += OnPanelChanged;
            _io2.PropertyChanged += OnPanelChanged;

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

        private void CompareDirectories(bool sync)
        {
            var vm = new CompareVM(_io1.ActualPath, _io2.ActualPath, Jobs, _dialogs, _logger, () => { _io1.FilterFiles(); _io2.FilterFiles(); });
            WindowHost.Show(() => new CompareDirsWindow(vm, sync));
        }

        // Loggers raise this from any thread; the collection is bound to the UI.
        private void AddLogInfo(LogInfo info, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var msg = new LogMsg(info, message);
                LogsCollection.Add(msg);
                CurrentLogMsg = msg;
                if (msg.IsError)
                    ErrorCount++;
            });
        }

        public void OnClose()
        {
            try
            {
                _configReader.SavePaths(_io1.ActualPath, _io2.ActualPath, _io1.TabPaths.ToArray(), _io2.TabPaths.ToArray());
            }
            catch (Exception e)
            {
                _logger.Error("Cannot save settings", e);
            }
        }

        // 0 Appearance, 1 Keyboard, 2 File operations (SettingsWindowVM.Section).
        private Task ShowSettings(int section) => WindowHost.ShowDialog(() =>
            new SettingsWindow(new SettingsWindowVM(KeyMap, _configReader) { Section = section }));

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

        private void OnPanelChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IOManager.CurrentOperation) && sender is IOManager panel && !string.IsNullOrEmpty(panel.CurrentOperation))
                Operation = panel.CurrentOperation;
        }

        // The log panel was opened: the error chip goes back to a plain "View log" button.
        public void MarkLogsSeen() => ErrorCount = 0;

        private void ClearLog()
        {
            LogsCollection.Clear();
            CurrentLogMsg = null;
            ErrorCount = 0;
        }


        #region Commands
        public ICommand SwitchCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand ClearLogCommand { get; set; }
        public ICommand SplitFileCommand { get; set; }
        public ICommand JoinFileCommand { get; set; }
        public ICommand CompareDirCommand { get; set; }
        public ICommand SyncDirsCommand { get; set; }
        public ICommand SettingsCommand { get; set; }
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

        public KeyMap KeyMap { get; }
        public JobQueue Jobs { get; }
        public ICommand ShowJobCommand { get; }
        public PaletteVM Palette { get; }

        public IOManager ActivePanel
        {
            get => _activePanel;
            set
            {
                if (_activePanel == value) return;
                if (_activePanel != null) _activePanel.IsActive = false;
                _activePanel = value;
                if (_activePanel != null) _activePanel.IsActive = true;
                OnPropertyChanged();
            }
        }

        public int ErrorCount
        {
            get => _errorCount;
            set
            {
                if (SetProperty(ref _errorCount, value))
                {
                    OnPropertyChanged(nameof(HasErrors));
                    OnPropertyChanged(nameof(LogButtonText));
                }
            }
        }

        public bool HasErrors => _errorCount > 0;

        public string LogButtonText => _errorCount switch
        {
            0 => "View log",
            1 => "1 error · View log",
            _ => _errorCount + " errors · View log"
        };

        private string _operation = string.Empty;
        // Last progress line from either panel (shown in the status bar while busy).
        public string Operation
        {
            get => _operation;
            set
            {
                if (SetProperty(ref _operation, value))
                    OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText => IsBusy ? "Busy · " + Operation : "Ready";

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (SetProperty(ref _isBusy, value))
                    OnPropertyChanged(nameof(StatusText));
            }
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