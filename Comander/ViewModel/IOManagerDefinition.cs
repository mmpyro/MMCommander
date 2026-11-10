using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Comander.CommanderIO;
using Comander.Core;
using Comander.View;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using IOLib;
using IOLinq;
using LogLib;
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
        private readonly IHistoryManager _historyManager;
        private readonly IConfigReader _configReader;
        private readonly IShortcutManager _shortcutManager;
        private readonly IPluginManager _pluginManager;
        private readonly IDialogService _dialogs;
        private IOManager _secondManager;
        private string _filter;
        private readonly IMessenger _messanger = WeakReferenceMessenger.Default;
        private IProxyIO _proxy;
        private ManagerType _type;
        private bool _focus;
        // Cancelled whenever a newer listing or filter starts, so a slow old result never replaces a newer one (BUG-11).
        private CancellationTokenSource _listing = new CancellationTokenSource();

        public IOManager(ManagerType type,string actualPath, IFileSystemManager fileManager, SyntaxParser syntaxParser, 
            IHistoryManager historyManager, IConfigReader configReader, 
            IPluginManager pluginManager, ILogger logger, IPathResolver pathResolver, IProxyIO proxy, IDialogService dialogs)
        {
            _proxy = proxy;
            _proxy.Manager = this;
            _type = type;
            _actualPath = actualPath;
            _fileManager = fileManager;
            _syntaxParser = syntaxParser;
            _historyManager = historyManager;
            _configReader = configReader;
            _shortcutManager = _configReader.ShortcutManager;
            _pluginManager = pluginManager;
            _logger = logger;
            _pathResolver = pathResolver;
            _dialogs = dialogs;
            ChooseDirCommand = new AsyncRelayCommand(ChooseDirectory);
            RootDirCommand = new RelayCommand(() => { if (Directory.Exists(ActualPath)) ActualPath = Path.GetPathRoot(Path.GetFullPath(ActualPath)); });
            ParentDirCommand = new RelayCommand(() =>
            {
                var dir = Directory.Exists(ActualPath) ? Directory.GetParent(ActualPath) : null;
                if (dir != null)
                    ActualPath = dir.FullName;
            });
            DeleteFileCommand = new AsyncRelayCommand(() => _proxy.DeleteFile(SelectedFiles()));
            CurrentOperation = string.Empty;
            CreateDirCommand = new AsyncRelayCommand(_proxy.CreateDirectory);
            CreateFileCommand = new AsyncRelayCommand(_proxy.CreateFile);
            ZipCommand = new AsyncRelayCommand(_proxy.ZipFiles);
            EnterIntoDirCommand = new RelayCommand(EnterIntoDir);
            RunFileCommand = new RelayCommand(() => _proxy.Run(SelectedFile));
            RunAsAdminCommand = new RelayCommand(() => _proxy.RunAsAdmin(SelectedFile));
            NotepadCommand = new RelayCommand(() => Shell.Run(_configReader["notepad"], SelectedFile.FullName, ActualPath));
            AddShortCutsCommand = new RelayCommand(() => _shortcutManager.Add(ActualPath));
            
            RefreshDriveCommand = new RelayCommand(() =>
            {
                LoadDrivers();
                _secondManager.LoadDrivers();
            });

            RefreshCommand = new RelayCommand(FilterFiles);
            ReverseSelectionCommand = new RelayCommand(ReversFileSelection);
            // Null when nothing is selected; the command then does nothing (BUG-33).
            SelectCommand = new RelayCommand<System.Collections.IList>(items =>
            {
                if (items == null)
                    return;
                foreach (IMetadataFileStructure item in items)
                    item.SelectFile();
                CountSelectedFiles();
            });
            SelectAllCommand = new RelayCommand(SelectAllFiles);
            SelectTypeCommand = new RelayCommand(SelectByType);
            CopyCommand = new AsyncRelayCommand(() => _proxy.CopyFile(SelectedFiles(), SeccondManagerCurrentDir));
            MoveCommand = new AsyncRelayCommand(() => _proxy.MoveFile(SelectedFiles(), SeccondManagerCurrentDir));
            ShowSortcutsCommand = new AsyncRelayCommand(ShowShortcutsWindow);
            HistoryCommand = new AsyncRelayCommand(ShowHistory);
            ConsoleRunCommand = new RelayCommand(() => Shell.Run(_configReader["cmd"], ActualPath, ActualPath));
            InfoCommand = new RelayCommand(() => WindowHost.Show(() => new InfoWindow(SelectedFile)));
            RenameCommand = new AsyncRelayCommand(() => _proxy.RenameFile(SelectedFile));
            UnZipCommand = new AsyncRelayCommand(_proxy.UnZipFiles);
            PluginCommand = new RelayCommand(ShowPluginWindow);
            SwitchFocusCommand = new RelayCommand(SwitchFocus);
            TreeWindowCommand = new RelayCommand(ShowTreeWindow);
            CopyToClipboardCommand = new AsyncRelayCommand(() => _dialogs.SetClipboardFiles(SelectedFiles().Select(t => t.FullName)));
            PasteToClipboardCommand = new AsyncRelayCommand(() => _proxy.PasteFromClipboard(CurrentDir));

            _messanger.Register<FocusMessage>(this, (_, msg) =>
                _focus = msg.ManagerName.Equals(_type.ToString(), StringComparison.OrdinalIgnoreCase));
            _messanger.Register<RefreshMessage>(this, (_, _) => Refresh());

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
        public ICommand SelectTypeCommand { get; set; }
        public ICommand RefreshDriveCommand { get; set; }
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
        public ICommand SwitchFocusCommand { get; set; }
        public ICommand TreeWindowCommand { get; set; }
        public ICommand CopyToClipboardCommand { get; set; }
        public ICommand PasteToClipboardCommand { get; set; }
        #endregion

    }
}