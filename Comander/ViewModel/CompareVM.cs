using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comander.Converters;
using Comander.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOLib;
using LogLib;

namespace Comander.ViewModel
{
    public class CompareRowVM
    {
        private static readonly DateConverter Dates = new DateConverter();

        public CompareRowVM(CompareRow row)
        {
            Row = row;
            LeftName = row.Left != null ? row.RelativePath : string.Empty;
            RightName = row.Right != null ? row.RelativePath : string.Empty;
            LeftSize = Size(row.Left);
            RightSize = Size(row.Right);
            LeftDate = Date(row.Left);
            RightDate = Date(row.Right);
            (Symbol, StatusLabel) = row.Kind switch
            {
                CompareKind.OnlyLeft => ("→", "Only on left: copy to right"),
                CompareKind.NewerLeft => ("→", "Newer on left: copy to right"),
                CompareKind.OnlyRight => ("←", "Only on right: copy to left"),
                CompareKind.NewerRight => ("←", "Newer on right: copy to left"),
                CompareKind.Different => ("≠", "Same date, different content: review"),
                _ => ("=", "Identical"),
            };
        }

        public CompareRow Row { get; }
        public string LeftName { get; }
        public string RightName { get; }
        public string LeftSize { get; }
        public string RightSize { get; }
        public string LeftDate { get; }
        public string RightDate { get; }
        public string Symbol { get; }
        public string StatusLabel { get; }
        public bool ToRight => Row.Kind is CompareKind.OnlyLeft or CompareKind.NewerLeft;
        public bool ToLeft => Row.Kind is CompareKind.OnlyRight or CompareKind.NewerRight;
        public bool IsDifferent => Row.Kind == CompareKind.Different;
        public bool IsIdentical => Row.Kind == CompareKind.Identical;
        public bool OnlyLeft => Row.Kind == CompareKind.OnlyLeft;
        public bool OnlyRight => Row.Kind == CompareKind.OnlyRight;

        private static string Size(CompareSide side) => side == null ? string.Empty : side.IsDirectory ? "—" : FileSizeConverter.Format(side.Size);
        private static string Date(CompareSide side) => side == null ? string.Empty : (string)Dates.Convert(side.Modified, typeof(string), null, null);
    }

    public class CompareChip
    {
        public string Label { get; init; }
        public int Count { get; init; }
    }

    // Compare directories window: scan both panes' folders, filter by status, synchronize (→ and ← only).
    public class CompareVM : ObservableObject
    {
        private readonly JobQueue _jobs;
        private readonly IDialogService _dialogs;
        private readonly ILogger _logger;
        private readonly Action _refreshPanels;
        private List<CompareRowVM> _all = new List<CompareRowVM>();
        private List<CompareRowVM> _rows = new List<CompareRowVM>();
        private List<CompareChip> _chips = new List<CompareChip>();
        private int _modeIndex, _filterIndex;
        private bool _recursive, _isScanning;
        private string _planText = string.Empty;

        public CompareVM(string left, string right, JobQueue jobs, IDialogService dialogs, ILogger logger, Action refreshPanels)
        {
            LeftPath = left;
            RightPath = right;
            _jobs = jobs;
            _dialogs = dialogs;
            _logger = logger;
            _refreshPanels = refreshPanels;
            RescanCommand = new AsyncRelayCommand(Rescan);
            SyncCommand = new AsyncRelayCommand(Synchronize, () => ToRight.Any() || ToLeft.Any());
        }

        public string LeftPath { get; }
        public string RightPath { get; }
        public AsyncRelayCommand RescanCommand { get; }
        public AsyncRelayCommand SyncCommand { get; }

        public int ModeIndex
        {
            get => _modeIndex;
            set { if (SetProperty(ref _modeIndex, value)) RescanCommand.Execute(null); }
        }

        public bool Recursive
        {
            get => _recursive;
            set { if (SetProperty(ref _recursive, value)) RescanCommand.Execute(null); }
        }

        // 0 All, 1 Different (incl. newer), 2 Only left, 3 Only right, 4 Identical.
        public int FilterIndex
        {
            get => _filterIndex;
            set { if (SetProperty(ref _filterIndex, Math.Max(0, value))) ApplyFilter(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            private set => SetProperty(ref _isScanning, value);
        }

        public List<CompareRowVM> Rows
        {
            get => _rows;
            private set => SetProperty(ref _rows, value);
        }

        public List<CompareChip> Chips
        {
            get => _chips;
            private set => SetProperty(ref _chips, value);
        }

        public string PlanText
        {
            get => _planText;
            private set => SetProperty(ref _planText, value);
        }

        private IEnumerable<CompareRowVM> ToRight => _all.Where(r => r.ToRight);
        private IEnumerable<CompareRowVM> ToLeft => _all.Where(r => r.ToLeft);

        public async Task Rescan()
        {
            IsScanning = true;
            try
            {
                var mode = (CompareMode)_modeIndex;
                var rows = await Task.Run(() => DirComparer.Compare(LeftPath, RightPath, mode, _recursive));
                _all = rows.Select(r => new CompareRowVM(r)).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Error("Cannot compare folders", ex);
                _all = new List<CompareRowVM>();
            }
            finally
            {
                IsScanning = false;
            }
            int diff = _all.Count(r => r.IsDifferent || r.Row.Kind is CompareKind.NewerLeft or CompareKind.NewerRight);
            Chips = new List<CompareChip>
            {
                new CompareChip { Label = "All", Count = _all.Count },
                new CompareChip { Label = "Different", Count = diff },
                new CompareChip { Label = "Only left", Count = _all.Count(r => r.OnlyLeft) },
                new CompareChip { Label = "Only right", Count = _all.Count(r => r.OnlyRight) },
                new CompareChip { Label = "Identical", Count = _all.Count(r => r.IsIdentical) },
            };
            OnPropertyChanged(nameof(FilterIndex));
            PlanText = string.Format("Plan: {0} → right · {1} ← left · {2} to review", ToRight.Count(), ToLeft.Count(), _all.Count(r => r.IsDifferent));
            ApplyFilter();
            SyncCommand.NotifyCanExecuteChanged();
        }

        private void ApplyFilter()
        {
            Rows = _all.Where(r => _filterIndex switch
            {
                1 => r.IsDifferent || r.Row.Kind is CompareKind.NewerLeft or CompareKind.NewerRight,
                2 => r.OnlyLeft,
                3 => r.OnlyRight,
                4 => r.IsIdentical,
                _ => true,
            }).ToList();
        }

        // Copies → rows to the right and ← rows to the left; ≠ rows are only listed for review.
        public async Task Synchronize()
        {
            var right = ToRight.ToList();
            var left = ToLeft.ToList();
            if (right.Count + left.Count == 0) return;
            if (!await _dialogs.Confirm(string.Format("Synchronize: copy {0} item(s) to the right and {1} to the left?", right.Count, left.Count)))
                return;
            var job = new TransferJob("Synchronizing", LeftPath + " ⇄ " + RightPath, j =>
            {
                Copy(j, right, RightPath);
                Copy(j, left, LeftPath);
            });
            var done = _jobs.Enqueue(job);
            _dialogs.ShowProgress(job);
            await done;
            _refreshPanels?.Invoke();
            await Rescan();
        }

        private void Copy(TransferJob job, List<CompareRowVM> rows, string targetRoot)
        {
            // Entries below a shared folder go to the same relative folder on the other side.
            foreach (var group in rows.GroupBy(r => Path.GetDirectoryName(r.Row.RelativePath.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty))
            {
                if (job.Token.IsCancellationRequested) return;
                var destination = Path.Combine(targetRoot, group.Key);
                var sources = group.Select(r => (r.ToRight ? r.Row.Left : r.Row.Right).FullName).ToList();
                var result = new TransferEngine(new TransferOptions { Policy = ConflictPolicy.OverwriteOlder }, null, job, job.Pause, job.Token).Copy(sources, destination);
                foreach (var error in result.Errors)
                {
                    job.AddError();
                    _logger.Error("Cannot synchronize " + error);
                }
            }
        }
    }
}
