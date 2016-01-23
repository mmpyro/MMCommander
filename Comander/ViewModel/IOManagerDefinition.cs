using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Input;
using Comander.CommanderIO;
using Comander.Core;
using Comander.ViewModel.Commands;
using IOLib;
using IOLinq;
using LogLib;
using RxFramework;
using Messanger;
using System.Windows;
using Comander.Messages;

namespace Comander.ViewModel
{
    public partial class IOManager
    {
        private readonly ILogger _logger;
        private readonly IPathResolver _pathResolver;
        private ObservableCollection<IMetadataFileStructure> _files;
        private ObservableCollection<DriveStruct> _drives;
        private string _actualPath;
        private readonly IFileSystemManager _fileManager;
        private readonly SyntaxParser _syntaxParser;
        private IMetadataFileStructure _selectedFile;
        private string _driveLetter;
        private string _currentOperation;
        private IMetadataFileStructure _rootDirectory;
        private readonly IHistoryManager _historyManager;
        private readonly IConfigReader _configReader;
        private readonly IShortcutManager _shortcutManager;
        private readonly IPluginManager _pluginManager;
        private IOManager _secondManager;
        private bool _isAvaiable = true;
        private string _filter;
        private Point _currentPosition;
        private ObservableCollection<IMetadataFileStructure> _selectedFiles;
        private IMessanger _messanger;
        private IOState _state;

        public IOManager(string actualPath, IFileSystemManager fileManager, SyntaxParser syntaxParser, 
            IHistoryManager historyManager, IConfigReader configReader, 
            IPluginManager pluginManager, ILogger logger, IPathResolver pathResolver)
        {
            _state = new IOUnBusyState(this, fileManager);
            _actualPath = actualPath;
            _fileManager = fileManager;
            _syntaxParser = syntaxParser;
            _historyManager = historyManager;
            _configReader = configReader;
            _shortcutManager = _configReader.ShortcutManager;
            _pluginManager = pluginManager;
            _logger = logger;
            _pathResolver = pathResolver;
            ChooseDirCommand = new DirChooseCommand(this);
            RootDirCommand = new RootDirCommand(this);
            ParentDirCommand = new ParentDirCommand(this);
            DeleteFileCommand = new ExecuteCommand(DeleteFile, _logger);
            CurrentOperation = string.Empty;
            CreateDirCommand = new ExecuteCommand(CreateDirectory, _logger);
            CreateFileCommand = new ExecuteCommand(CreateFile, _logger);
            ZipCommand = new ExecuteCommand(ZipFiles, _logger);
            EnterIntoDirCommand = new ExecuteCommand(EnterIntoDir, _logger);
            RunFileCommand = new ExecuteCommand(Run, _logger);
            RunAsAdminCommand = new ExecuteCommand(RunAsAdmin, _logger);
            NotepadCommand = new ExecuteCommand(() => Process.Start(_configReader["notepad"], SelectedFile.FullName), _logger);
            AddShortCutsCommand = new ExecuteCommand(() => _shortcutManager.Add(ActualPath), _logger);
            
            RefreshDriveCommand = new ExecuteCommand(() =>
            {
                LoadDrivers();
                _secondManager.LoadDrivers();
            }, _logger);

            RefreshCommand = new ExecuteCommand(Refresh, _logger);
            ReverseSelectionCommand = new ExecuteCommand(ReversFileSelection, _logger);
            SelectCommand = new SelectFilesCommand(RefreshFilesCollection);
            SelectAllCommand = new ExecuteCommand(SelectAllFiles, _logger);
            CopyCommand = new ExecuteCommand(CopyFile, _logger);
            MoveCommand = new ExecuteCommand(MoveFile, _logger);
            ShowSortcutsCommand = new ExecuteCommand(ShowShortcutsWindow, _logger);
            HistoryCommand = new ExecuteCommand(ShowHistory, _logger);
            ConsoleRunCommand = new ExecuteCommand(RunConsole, _logger);
            InfoCommand = new ExecuteCommand(ShowInfoWindow, _logger);
            RenameCommand = new ExecuteCommand(RenameFile, _logger);
            UnZipCommand = new ExecuteCommand(UnZipFiles, _logger);
            PluginCommand = new ExecuteCommand(ShowPluginWindow,_logger);

            _messanger = Messanger.Messanger.GetInstance();
            _messanger.Register(typeof(WindowPositionEventArgs), MouseMoveCallback);

            ObservableFromProperty<string>("Filter")
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromSeconds(1))
                .InvokeCommand(new ReactiveCommand<string>(t => FilterFiles(), _ => true));

            LoadDrivers();
            Refresh();
        }

        #region ICommand
        public ICommand ChooseDirCommand { get; set; }
        public ICommand RootDirCommand { get; set; }
        public ICommand ParentDirCommand { get; set; }
        public ICommand DeleteFileCommand { get; set; }
        public ICommand CreateDirCommand { get; set; }
        public ICommand CreateFileCommand { get; set; }
        public ICommand EnterIntoDirCommand { get; set; }
        public ICommand RunFileCommand { get; set; }
        public ICommand NotepadCommand { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand MoveCommand { get; set; }
        public ICommand SelectCommand { get; set; }
        public ICommand ReverseSelectionCommand { get; set; }
        public ICommand SelectAllCommand { get; set; }
        public ICommand RefreshDriveCommand { get; set; }
        public ICommand SplitFileCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand ZipCommand { get; set; }
        public ICommand RunAsAdminCommand { get; set; }
        public ICommand AddShortCutsCommand { get; set; }
        public ICommand ShowSortcutsCommand { get; set; }
        public ICommand HistoryCommand { get; set; }
        public ICommand ConsoleRunCommand { get; set; }
        public ICommand InfoCommand { get; set; }
        public ICommand RenameCommand { get; set; }
        public ICommand UnZipCommand { get; set; }
        public ICommand PluginCommand { get; set; }
        #endregion

    }
}