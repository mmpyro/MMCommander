using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Comander.Annotations;
using Comander.CommanderIO;
using Comander.Core;
using Comander.View;
using Comander.ViewModel.Commands;
using IOLib;
using LogLib;
using Search;

namespace Comander.ViewModel
{
    public class MainVM : INotifyPropertyChanged
    {
        private IOManager _io1;
        private IOManager _io2;
        private readonly ConfigReader _configReader;
        private ObservableCollection<LogMsg> _logsCollection = new ObservableCollection<LogMsg>();
        private LogMsg _currentLogMsg;
        private readonly GUILogger _logger;
        private int _current = 0;
        private string _logCount;

        public MainVM(IOManager io1, IOManager io2, ConfigReader configReader ,GUILogger logger)
        {
            _configReader = configReader;
            _logger = logger;
            _logger.NotifyEvent += AddLogInfo;
            _io1 = io1;
            _io2 = io2;

            SwitchCommand = new ExecuteCommand(SwitchCommanders, _logger);
            RefreshCommand = new ExecuteCommand(RefreshCommanders, _logger);
            TreeCommand = new ExecuteCommand(ShowTreeWindow, _logger);
            SplitFileCommand = new ExecuteCommand(SplitFile, _logger);
            JoinFileCommand = new ExecuteCommand(JoinFile, _logger);
            RunNotepadCommand = new ExecuteCommand(() => Process.Start(_configReader["notepad"]), _logger);
            RunCmdCommand = new ExecuteCommand(() =>
            {
                var processInfo = new ProcessStartInfo(_configReader["cmd"]);
                processInfo.WorkingDirectory = @"C:";
                Process.Start(processInfo);
            }, _logger);

            ClearLogCommand = new ExecuteCommand(ClearLog);
            NextLogCommand = new ExecuteCommand(NextLog);
            PreviusLogCommand = new ExecuteCommand(BackLog);
            CompareDirCommand = new ExecuteCommand(CompareDirectories);
            GenericCommand = new GenericCommand(Io1, Io2, _logger);
            AboutCommand = new ExecuteCommand(() => (new AboutWindow(string.Format("Product version: {0}\nCreated by Michał Marszałek.", 
                AssemblyVersionResolver.GetProductVersion(this.GetType())))).ShowDialog());
            KeyMapCommand = new ExecuteCommand( () => Process.Start(_configReader["web"],@".\Manuals\keymap.html"),_logger);
            SearchCommand = new ExecuteCommand(Search,_logger);
        }

        private void Search()
        {
            try
            {
                SearchWindow window = new SearchWindow(_io1);
                window.Show();
            }
            catch (Exception e)
            {
                _logger.WriteLine(e, LogInfo.Error);
            }
        }

        private void CompareDirectories()
        {
            var fileMarker = new FileMarker(_io1.Files, _io2.Files, new FileParametersComparer());
            _io1.Files = new ObservableCollection<IMetadataFileStructure>(fileMarker.Mark());
        }

        private void AddLogInfo(LogInfo info, string message)
        {
            LogsCollection.Add(new LogMsg(info, message));
            NextLog();
        }

        public void BeforeClose()
        {
            try
            {
                _configReader.SavePaths(_io1.ActualPath, _io2.ActualPath);
            }
            catch
            {
            }
        }

        private void SwitchCommanders()
        {
            string bufforPath = _io1.ActualPath;
            _io1.ActualPath = _io2.ActualPath;
            _io2.ActualPath = bufforPath;
        }

        private void RefreshCommanders()
        {
            _io1.LoadDrivers();
            _io2.LoadDrivers();
        }

        private void ShowTreeWindow()
        {
            TreeWindow window = new TreeWindow();
            window.Show();
        }

        private void SplitFile()
        {
            SplitWindow splitWindow = new SplitWindow(_logger);
            splitWindow.Show();
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
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine("Join was finished without errors.", LogInfo.Info,
                        LogLevel.Minimal));
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => _logger.WriteLine(ex, LogInfo.Info, LogLevel.Minimal));
                }
            });
        }

        private void JoinFile()
        {
            JoinWindow joinWindow = new JoinWindow();
            joinWindow.ShowDialog();
            if (joinWindow.JoinParameter != null)
            {
                _logger.WriteLine("Join in progress...", LogInfo.Info, LogLevel.Minimal);
                JoinFileProcessor(joinWindow.JoinParameter.JoinPath, 
                    joinWindow.JoinParameter.DestinationPath, joinWindow.JoinParameter.FileName);
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
        }



        #region Commands
        public ICommand RunNotepadCommand { get; set; }
        public ICommand RunCmdCommand { get; set; }
        public ICommand SwitchCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand TreeCommand { get; set; }
        public ICommand ClearLogCommand { get; set; }
        public ICommand SplitFileCommand { get; set; }
        public ICommand JoinFileCommand { get; set; }
        public ICommand NextLogCommand { get; set; }
        public ICommand PreviusLogCommand { get; set; }
        public ICommand CompareDirCommand { get; set; }
        public ICommand GenericCommand { get; set; }
        public ICommand AboutCommand { get; set; }
        public ICommand KeyMapCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        #endregion

        #region Property

        public IOManager Io1
        {
            get { return _io1; }
            set
            {
                _io1 = value;
                OnPropertyChanged("Io1");
            }
        }

        public IOManager Io2
        {
            get { return _io2; }
            set
            {
                _io2 = value;
                OnPropertyChanged("Io2");
            }
        }

        public ObservableCollection<LogMsg> LogsCollection
        {
            get { return _logsCollection; }
            set
            {
                _logsCollection = value;
                OnPropertyChanged("LogsCollection");
            }
        }

        public LogMsg CurrentLogMsg
        {
            get { return _currentLogMsg; }
            set
            {
                if (Equals(value, _currentLogMsg)) return;
                _currentLogMsg = value;
                OnPropertyChanged("CurrentLogMsg");
            }
        }

        public string LogCount
        {
            get { return _logCount; }
            set
            {
                if (value == _logCount) return;
                _logCount = value;
                OnPropertyChanged("LogCount");
            }
        }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}