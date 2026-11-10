using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using IOLib;
using Comander.Converters;

namespace Comander.ViewModel
{
    // Folder tabs, back/forward history, breadcrumb and footer summary of one panel.
    public partial class IOManager
    {
        private PaneTab _activeTab;
        private string _historyTarget;
        private List<PathSegment> _segments = new List<PathSegment>();
        private string _summaryText = string.Empty;
        private string _selectionText = string.Empty;
        private DriveSpace _space;

        public ObservableCollection<PaneTab> Tabs { get; } = new ObservableCollection<PaneTab>();

        public PaneTab ActiveTab
        {
            get => _activeTab;
            private set
            {
                if (_activeTab == value) return;
                if (_activeTab != null) _activeTab.IsActive = false;
                _activeTab = value;
                if (_activeTab != null) _activeTab.IsActive = true;
                OnPropertyChanged();
            }
        }

        public RelayCommand BackCommand { get; private set; }
        public RelayCommand ForwardCommand { get; private set; }
        public RelayCommand NewTabCommand { get; private set; }
        public RelayCommand CloseTabCommand { get; private set; }
        public RelayCommand NextTabCommand { get; private set; }
        public RelayCommand<PaneTab> SelectTabCommand { get; private set; }
        public RelayCommand<string> NavigateCommand { get; private set; }
        public RelayCommand ToggleViewCommand { get; private set; }

        private void InitTabs(string path)
        {
            BackCommand = new RelayCommand(GoBack, () => ActiveTab?.Back.Count > 0);
            ForwardCommand = new RelayCommand(GoForward, () => ActiveTab?.Forward.Count > 0);
            NewTabCommand = new RelayCommand(() => OpenTab(ActualPath));
            CloseTabCommand = new RelayCommand(CloseActiveTab, () => Tabs.Count > 1);
            NextTabCommand = new RelayCommand(() => SelectTab(Tabs[(Tabs.IndexOf(ActiveTab) + 1) % Tabs.Count]), () => Tabs.Count > 1);
            SelectTabCommand = new RelayCommand<PaneTab>(SelectTab);
            ToggleViewCommand = new RelayCommand(() => ViewIndex = 1 - ViewIndex);
            NavigateCommand = new RelayCommand<string>(p => { if (!string.IsNullOrEmpty(p)) ActualPath = p; });
            var tab = new PaneTab(path);
            Tabs.Add(tab);
            ActiveTab = tab;
            Segments = PathSegment.Split(path);
        }

        // Restores saved tabs (Config.xml). Missing folders are skipped; the active tab keeps the current path.
        public void RestoreTabs(IEnumerable<string> paths)
        {
            foreach (var p in paths ?? Enumerable.Empty<string>())
                if (System.IO.Directory.Exists(p) && Tabs.All(t => t.Path != p))
                    Tabs.Add(new PaneTab(p));
            RefreshTabCommands();
        }

        public IEnumerable<string> TabPaths => Tabs.Select(t => t.Path);

        public void OpenTab(string path)
        {
            var tab = new PaneTab(path);
            Tabs.Insert(Tabs.IndexOf(ActiveTab) + 1, tab);
            SelectTab(tab);
        }

        public void SelectTab(PaneTab tab)
        {
            if (tab == null || !Tabs.Contains(tab)) return;
            ActiveTab = tab;
            _historyTarget = tab.Path;
            ActualPath = tab.Path;
            RefreshTabCommands();
        }

        private void CloseActiveTab()
        {
            if (Tabs.Count < 2) return;
            var index = Tabs.IndexOf(ActiveTab);
            Tabs.RemoveAt(index);
            SelectTab(Tabs[System.Math.Min(index, Tabs.Count - 1)]);
        }

        private void GoBack()
        {
            if (ActiveTab.Back.Count == 0) return;
            ActiveTab.Forward.Push(ActiveTab.Path);
            NavigateHistory(ActiveTab.Back.Pop());
        }

        private void GoForward()
        {
            if (ActiveTab.Forward.Count == 0) return;
            ActiveTab.Back.Push(ActiveTab.Path);
            NavigateHistory(ActiveTab.Forward.Pop());
        }

        private void NavigateHistory(string path)
        {
            _historyTarget = path;
            ActualPath = path;
            RefreshTabCommands();
        }

        // Called after a folder listed successfully. Only real folders enter history, not every keystroke in the path box.
        private void OnListed(string path)
        {
            var tab = ActiveTab;
            if (tab != null && tab.Path != path)
            {
                if (_historyTarget != path)
                {
                    tab.Back.Push(tab.Path);
                    tab.Forward.Clear();
                }
                tab.Path = path;
            }
            _historyTarget = null;
            Segments = PathSegment.Split(path);
            RefreshTabCommands();
        }

        private void RefreshTabCommands()
        {
            BackCommand.NotifyCanExecuteChanged();
            ForwardCommand.NotifyCanExecuteChanged();
            CloseTabCommand.NotifyCanExecuteChanged();
            NextTabCommand.NotifyCanExecuteChanged();
        }

        private int _viewIndex;
        // 0 = details (the DataGrid), 1 = thumbnails. Per panel.
        public int ViewIndex
        {
            get => _viewIndex;
            set
            {
                if (SetProperty(ref _viewIndex, value is 1 ? 1 : 0))
                    OnPropertyChanged(nameof(IsThumbnails));
            }
        }

        public bool IsThumbnails => _viewIndex == 1;

        public List<PathSegment> Segments
        {
            get => _segments;
            private set => SetProperty(ref _segments, value);
        }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
        }

        public string SelectionText
        {
            get => _selectionText;
            private set => SetProperty(ref _selectionText, value);
        }

        public DriveSpace Space
        {
            get => _space;
            private set
            {
                if (SetProperty(ref _space, value))
                {
                    OnPropertyChanged(nameof(SpaceText));
                    OnPropertyChanged(nameof(DriveLabel));
                }
            }
        }

        public string SpaceText => _space == null ? string.Empty
            : FileSizeConverter.FormatSpace(_space.Free) + " free of " + FileSizeConverter.FormatSpace(_space.Total);

        public string DriveLabel => _space?.Label ?? System.IO.Path.GetPathRoot(ActualPath ?? string.Empty);

        private void UpdateSummary()
        {
            var files = Files ?? new ObservableCollection<CommanderIO.IMetadataFileStructure>();
            int dirs = files.Count(f => f.IsDirectory && f.Name != "..");
            int plain = files.Count(f => !f.IsDirectory);
            SummaryText = Plural(dirs, "folder") + " · " + Plural(plain, "file");
            var marked = files.Where(f => f.IsSelected()).ToList();
            SelectionText = marked.Count == 0 ? string.Empty
                : marked.Count + " selected · " + FileSizeConverter.Format(marked.Sum(f => f.IsDirectory ? f.MetaData?.FolderSize ?? 0 : f.Size));
        }

        private void ComputeFolderSize(CommanderIO.IMetadataFileStructure dir)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                long size;
                try { size = TransferEngine.FolderSize(dir.FullName); }
                catch (System.Exception ex) when (ex is System.IO.IOException or System.UnauthorizedAccessException) { return; }
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    dir.MetaData.FolderSize = size;
                    UpdateSummary();
                });
            });
        }

        private static string Plural(int n, string word) => n + " " + word + (n == 1 ? string.Empty : "s");
    }
}
