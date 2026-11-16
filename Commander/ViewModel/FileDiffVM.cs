using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commander.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOLib;
using LogLib;

namespace Commander.ViewModel
{
    // One aligned row of the file compare window. A side without a line here has No = -1 and empty text.
    public class DiffRowVM : ObservableObject
    {
        private static readonly IReadOnlyList<(int Start, int Length)> None = new (int, int)[0];
        private bool _isEditingLeft, _isEditingRight;
        private string _editText;

        public DiffKind Kind { get; init; }
        public int LeftNo { get; init; } = -1;
        public int RightNo { get; init; } = -1;
        public string LeftLabel { get; init; } = string.Empty;
        public string RightLabel { get; init; } = string.Empty;
        public string Left { get; init; } = string.Empty;
        public string Right { get; init; } = string.Empty;
        public IReadOnlyList<(int Start, int Length)> LeftSpans { get; init; } = None;
        public IReadOnlyList<(int Start, int Length)> RightSpans { get; init; } = None;

        public bool IsSame => Kind == DiffKind.Same;
        public bool IsChanged => Kind == DiffKind.Changed;
        public bool IsOnlyLeft => Kind == DiffKind.OnlyLeft;
        public bool IsOnlyRight => Kind == DiffKind.OnlyRight;
        public bool IsLeftFiller => LeftNo < 0;
        public bool IsRightFiller => RightNo < 0;

        public bool IsEditingLeft
        {
            get => _isEditingLeft;
            set => SetProperty(ref _isEditingLeft, value);
        }

        public bool IsEditingRight
        {
            get => _isEditingRight;
            set => SetProperty(ref _isEditingRight, value);
        }

        public string EditText
        {
            get => _editText;
            set => SetProperty(ref _editText, value);
        }

        public int LineOn(int side) => side == 0 ? LeftNo : RightNo;
        public string TextOn(int side) => side == 0 ? Left : Right;

        public static DiffRowVM From(DiffRow row)
        {
            IReadOnlyList<(int, int)> inLeft = None, inRight = None;
            if (row.Kind == DiffKind.Changed)
            {
                TextDiff.Spans(row.Left, row.Right, out var a, out var b);
                inLeft = a;
                inRight = b;
            }
            return new DiffRowVM
            {
                Kind = row.Kind,
                LeftNo = row.LeftNo,
                RightNo = row.RightNo,
                LeftLabel = row.LeftNo < 0 ? string.Empty : (row.LeftNo + 1).ToString(),
                RightLabel = row.RightNo < 0 ? string.Empty : (row.RightNo + 1).ToString(),
                Left = row.Left ?? string.Empty,
                Right = row.Right ?? string.Empty,
                LeftSpans = inLeft,
                RightSpans = inRight,
            };
        }
    }

    // Compare files window (Shift+F3): side-by-side line diff of two text files, editable and savable;
    // a read-only hex compare when either file is not text.
    public class FileDiffVM : ObservableObject
    {
        private const int UndoLimit = 100;
        // shortcut: the hex view lists only the first 4 MB (the byte count covers the whole files). Upgrade to paging if needed.
        public const int BinaryShown = 4 * 1024 * 1024;
        private const int HexWidth = 16, AsciiAt = HexWidth * 3 + 1;
        // shortcut: the scroll range assumes about 7.3 px per character of the 12 px mono font.
        private const double CharWidth = 7.3;

        private readonly IDialogService _dialogs;
        private readonly ILogger _logger;
        private readonly Action _refreshPanels;
        private readonly TextDoc[] _docs = new TextDoc[2];
        private readonly bool[] _dirty = new bool[2];
        private readonly List<Snapshot> _undo = new List<Snapshot>();
        private List<DiffRowVM> _rows = new List<DiffRowVM>();
        private DiffRowVM _editing;
        private int _editSide, _cursor = -1, _side, _diffCount, _loadId, _editCaret;
        private bool _isBinary, _isLoading;
        private string _status = string.Empty;
        private double _hOffset, _scrollMax;

        private class Snapshot
        {
            public (List<string>, List<string>)[] Docs;
            public bool[] Dirty;
            public int Side, Line;
        }

        public FileDiffVM(string left, string right, IDialogService dialogs, ILogger logger, Action refreshPanels)
        {
            LeftPath = left;
            RightPath = right;
            _dialogs = dialogs;
            _logger = logger;
            _refreshPanels = refreshPanels;
            NextDiffCommand = new RelayCommand(NextDiff);
            PrevDiffCommand = new RelayCommand(PrevDiff);
            CopyToRightCommand = new RelayCommand(() => CopyBlock(1), () => !IsBinary);
            CopyToLeftCommand = new RelayCommand(() => CopyBlock(0), () => !IsBinary);
            UndoCommand = new RelayCommand(Undo, () => _undo.Count > 0);
            SaveCommand = new AsyncRelayCommand(Save, () => HasChanges);
            ReloadCommand = new AsyncRelayCommand(Reload);
        }

        public string LeftPath { get; }
        public string RightPath { get; }
        public RelayCommand NextDiffCommand { get; }
        public RelayCommand PrevDiffCommand { get; }
        public RelayCommand CopyToRightCommand { get; }
        public RelayCommand CopyToLeftCommand { get; }
        public RelayCommand UndoCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public List<DiffRowVM> Rows
        {
            get => _rows;
            private set => SetProperty(ref _rows, value);
        }

        public int CursorIndex
        {
            get => _cursor;
            set => SetProperty(ref _cursor, value);
        }

        // 0 = left, 1 = right: the side edits and Copy act on.
        public int ActiveSide
        {
            get => _side;
            set
            {
                if (!SetProperty(ref _side, value)) return;
                OnPropertyChanged(nameof(IsLeftActive));
                OnPropertyChanged(nameof(IsRightActive));
            }
        }

        public bool IsLeftActive => _side == 0;
        public bool IsRightActive => _side == 1;

        public int DiffCount
        {
            get => _diffCount;
            private set => SetProperty(ref _diffCount, value);
        }

        public bool IsBinary
        {
            get => _isBinary;
            private set => SetProperty(ref _isBinary, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        // Shared horizontal scroll of both sides, in pixels.
        public double HOffset
        {
            get => _hOffset;
            set => SetProperty(ref _hOffset, Math.Clamp(value, 0, _scrollMax));
        }

        public double ScrollMax
        {
            get => _scrollMax;
            private set => SetProperty(ref _scrollMax, value);
        }

        public bool LeftDirty => _dirty[0];
        public bool RightDirty => _dirty[1];
        public bool HasChanges => _dirty[0] || _dirty[1];
        public bool IsEditing => _editing != null;
        // Caret the view puts in the edit box when it opens.
        public int EditCaret => _editCaret;
        public DiffRowVM Current => _cursor >= 0 && _cursor < _rows.Count ? _rows[_cursor] : null;

        public async Task Load()
        {
            int load = ++_loadId;
            IsLoading = true;
            try
            {
                var (left, right) = await Task.Run(() => (TextDoc.Load(LeftPath), TextDoc.Load(RightPath)));
                if (load != _loadId) return;
                _undo.Clear();
                _dirty[0] = _dirty[1] = false;
                _editing = null;
                if (left != null && right != null)
                {
                    _docs[0] = left;
                    _docs[1] = right;
                    IsBinary = false;
                    var rows = await Task.Run(() => TextRows());
                    if (load != _loadId) return;
                    Show(rows);
                }
                else
                {
                    _docs[0] = _docs[1] = null;
                    IsBinary = true;
                    var (rows, status) = await Task.Run(() => BinaryRows(LeftPath, RightPath));
                    if (load != _loadId) return;
                    Show(rows);
                    Status = status;
                }
                CursorIndex = Math.Max(0, _rows.FindIndex(r => !r.IsSame));
                HOffset = 0;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (load != _loadId) return;
                _logger.Error("Cannot compare files", ex);
                Rows = new List<DiffRowVM>();
                Status = "Cannot read the files: " + ex.Message;
            }
            finally
            {
                if (load == _loadId) IsLoading = false;
                Changed();
            }
        }

        private List<DiffRowVM> TextRows() => TextDiff.Lines(_docs[0].Lines, _docs[1].Lines).Select(DiffRowVM.From).ToList();

        private void Show(List<DiffRowVM> rows)
        {
            Rows = rows;
            int count = 0;
            for (int i = 0; i < rows.Count; i++)
                if (!rows[i].IsSame && (i == 0 || rows[i - 1].IsSame)) count++;
            DiffCount = count;
            int longest = rows.Count == 0 ? 0 : rows.Max(r => Math.Max(r.Left.Length, r.Right.Length));
            ScrollMax = longest * CharWidth;
            if (IsBinary) return;
            Status = count switch
            {
                0 when _docs[0].ToBytes().AsSpan().SequenceEqual(_docs[1].ToBytes()) => "Files are identical",
                0 => "Same text; line endings or encoding differ",
                1 => "1 difference",
                _ => count + " differences",
            };
        }

        private void Changed()
        {
            OnPropertyChanged(nameof(LeftDirty));
            OnPropertyChanged(nameof(RightDirty));
            OnPropertyChanged(nameof(HasChanges));
            OnPropertyChanged(nameof(IsEditing));
            CopyToLeftCommand.NotifyCanExecuteChanged();
            CopyToRightCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
        }

        #region Navigation

        // A difference is a run of rows that are not the same.
        public void NextDiff()
        {
            int i = Math.Max(_cursor, 0);
            if (_cursor >= 0)
                while (i < _rows.Count && !_rows[i].IsSame) i++;
            while (i < _rows.Count && _rows[i].IsSame) i++;
            if (i < _rows.Count) CursorIndex = i;
        }

        public void PrevDiff()
        {
            if (_cursor < 0) return;
            int i = BlockStart(_cursor) - 1;
            while (i >= 0 && _rows[i].IsSame) i--;
            if (i >= 0) CursorIndex = BlockStart(i);
        }

        private int BlockStart(int i)
        {
            if (_rows[i].IsSame) return i;
            while (i > 0 && !_rows[i - 1].IsSame) i--;
            return i;
        }

        public void Move(int rows)
        {
            if (_rows.Count > 0) CursorIndex = Math.Clamp(_cursor + rows, 0, _rows.Count - 1);
        }

        #endregion

        #region Editing

        public bool BeginEdit(int caret = int.MaxValue)
        {
            var row = Current;
            if (IsBinary || row == null) return false;
            CancelEdit();
            _editing = row;
            _editSide = _side;
            row.EditText = row.TextOn(_side);
            _editCaret = Math.Min(caret, row.EditText.Length);
            if (_side == 0) row.IsEditingLeft = true;
            else row.IsEditingRight = true;
            OnPropertyChanged(nameof(IsEditing));
            return true;
        }

        public void CancelEdit()
        {
            if (_editing == null) return;
            _editing.IsEditingLeft = _editing.IsEditingRight = false;
            _editing = null;
            OnPropertyChanged(nameof(IsEditing));
        }

        // Writes the edit box back; a pasted line break becomes several lines.
        public void CommitEdit()
        {
            if (_editing == null) return;
            var row = _editing;
            var text = row.EditText ?? string.Empty;
            CancelEdit();
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int no = row.LineOn(_editSide);
            if (no >= 0 && text == _docs[_editSide].Lines[no]) return;
            if (no < 0 && text.Length == 0) return;
            int at = no >= 0 ? no : InsertIndex(_rows.IndexOf(row), _editSide);
            Replace(_editSide, at, no >= 0 ? 1 : 0, lines, at + lines.Length - 1);
        }

        // Enter in the edit box: the text after the caret moves to a new line below, which is edited next.
        public void SplitLine(int caret)
        {
            if (_editing == null) return;
            var row = _editing;
            var text = row.EditText ?? string.Empty;
            caret = Math.Clamp(caret, 0, text.Length);
            CancelEdit();
            int no = row.LineOn(_side);
            int at = no >= 0 ? no : InsertIndex(_rows.IndexOf(row), _side);
            Replace(_side, at, no >= 0 ? 1 : 0, new[] { text.Substring(0, caret), text.Substring(caret) }, at + 1);
            BeginEdit(0);
        }

        // Backspace at the start of the edit box: the line joins the one above.
        public void JoinLine()
        {
            if (_editing == null) return;
            var row = _editing;
            var text = row.EditText ?? string.Empty;
            int no = row.LineOn(_side);
            int at = no >= 0 ? no : InsertIndex(_rows.IndexOf(row), _side);
            if (at == 0) return;
            CancelEdit();
            var above = _docs[_side].Lines[at - 1];
            Replace(_side, at - 1, no >= 0 ? 2 : 1, new[] { above + text }, at - 1);
            BeginEdit(above.Length);
        }

        public void DeleteLine()
        {
            CancelEdit();
            var row = Current;
            if (IsBinary || row == null || row.LineOn(_side) < 0) return;
            int no = row.LineOn(_side);
            Replace(_side, no, 1, new string[0], no);
        }

        // Makes the target side of the difference under the cursor equal to the other side.
        public void CopyBlock(int toSide)
        {
            CommitEdit();
            if (IsBinary || Current == null || Current.IsSame) return;
            int start = BlockStart(_cursor), end = start;
            while (end < _rows.Count && !_rows[end].IsSame) end++;
            var block = _rows.GetRange(start, end - start);
            int from = 1 - toSide;
            var source = block.Where(r => r.LineOn(from) >= 0).Select(r => r.TextOn(from)).ToList();
            int remove = block.Count(r => r.LineOn(toSide) >= 0);
            int at = InsertIndex(start, toSide);
            Replace(toSide, at, remove, source, at);
        }

        // Where a line typed on a side's empty row goes: before the next line that side has.
        private int InsertIndex(int rowIndex, int side)
        {
            for (int i = Math.Max(rowIndex, 0); i < _rows.Count; i++)
                if (_rows[i].LineOn(side) >= 0) return _rows[i].LineOn(side);
            return _docs[side].Lines.Count;
        }

        // Every edit goes through here: undo point, change, re-compare, cursor back on focusLine of that side.
        private void Replace(int side, int at, int remove, IList<string> lines, int focusLine)
        {
            _undo.Add(new Snapshot { Docs = new[] { _docs[0].Snapshot(), _docs[1].Snapshot() }, Dirty = (bool[])_dirty.Clone(), Side = _side, Line = Current?.LineOn(_side) ?? 0 });
            if (_undo.Count > UndoLimit) _undo.RemoveAt(0);
            _docs[side].Replace(at, remove, lines);
            _dirty[side] = true;
            Rebuild(side, focusLine);
        }

        // shortcut: re-compares the whole files after each edit, on the UI thread; prefix/suffix trimming keeps it fast for small edits. Upgrade to a background diff if big files lag.
        private void Rebuild(int side, int line)
        {
            Show(TextRows());
            int index = _rows.FindIndex(r => r.LineOn(side) >= line);
            CursorIndex = index >= 0 ? index : _rows.Count - 1;
            ActiveSide = side;
            Changed();
        }

        public void Undo()
        {
            CancelEdit();
            if (_undo.Count == 0) return;
            var last = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            _docs[0].Restore(last.Docs[0]);
            _docs[1].Restore(last.Docs[1]);
            last.Dirty.CopyTo(_dirty, 0);
            Rebuild(last.Side, last.Line);
        }

        #endregion

        public async Task Save()
        {
            CommitEdit();
            for (int side = 0; side < 2; side++)
            {
                var doc = _docs[side];
                if (!_dirty[side] || doc == null) continue;
                if (doc.ChangedOnDisk && !await _dialogs.Confirm(Path.GetFileName(doc.Path) + " was changed on disk after it was opened. Overwrite it?"))
                    continue;
                try
                {
                    doc.Save();
                    _dirty[side] = false;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.Error("Cannot save " + doc.Path + " (read-only or no permission)", ex);
                }
            }
            Changed();
            _refreshPanels?.Invoke();
        }

        // F5: read both files again; unsaved edits are dropped only after asking.
        public async Task Reload()
        {
            CommitEdit();
            if (HasChanges && !await _dialogs.Confirm("Discard unsaved changes and compare the files again?")) return;
            await Load();
        }

        public async Task<bool> ConfirmDiscard() => !HasChanges || await _dialogs.Confirm("Discard unsaved changes to the compared files?");

        #region Binary

        private static (List<DiffRowVM>, string) BinaryRows(string leftPath, string rightPath)
        {
            using var left = System.IO.File.OpenRead(leftPath);
            using var right = System.IO.File.OpenRead(rightPath);
            var a = new byte[(int)Math.Min(left.Length, BinaryShown)];
            var b = new byte[(int)Math.Min(right.Length, BinaryShown)];
            left.ReadExactly(a);
            right.ReadExactly(b);

            var rows = new List<DiffRowVM>();
            for (int off = 0; off < Math.Max(a.Length, b.Length); off += HexWidth)
            {
                var inA = new List<(int, int)>();
                var inB = new List<(int, int)>();
                for (int i = off; i < off + HexWidth; i++)
                {
                    bool hasA = i < a.Length, hasB = i < b.Length;
                    if (hasA == hasB && (!hasA || a[i] == b[i])) continue;
                    if (hasA) { inA.Add(((i - off) * 3, 2)); inA.Add((AsciiAt + i - off, 1)); }
                    if (hasB) { inB.Add(((i - off) * 3, 2)); inB.Add((AsciiAt + i - off, 1)); }
                }
                bool onA = off < a.Length, onB = off < b.Length;
                rows.Add(new DiffRowVM
                {
                    Kind = !onB ? DiffKind.OnlyLeft : !onA ? DiffKind.OnlyRight : inA.Count > 0 ? DiffKind.Changed : DiffKind.Same,
                    LeftNo = onA ? off : -1,
                    RightNo = onB ? off : -1,
                    LeftLabel = onA ? off.ToString("X8") : string.Empty,
                    RightLabel = onB ? off.ToString("X8") : string.Empty,
                    Left = onA ? Hex(a, off) : string.Empty,
                    Right = onB ? Hex(b, off) : string.Empty,
                    LeftSpans = inA,
                    RightSpans = inB,
                });
            }

            var (differing, first) = CountDifferences(left, right);
            string status = differing == 0 ? "Files are identical"
                : string.Format("Binary: {0:N0} bytes differ, first at 0x{1:X}", differing, first);
            if (left.Length != right.Length) status += string.Format(" · sizes {0:N0} and {1:N0} bytes", left.Length, right.Length);
            if (Math.Max(left.Length, right.Length) > BinaryShown) status += " · showing the first 4 MB";
            return (rows, status);
        }

        private static string Hex(byte[] data, int off)
        {
            var text = new StringBuilder(AsciiAt + HexWidth);
            for (int i = off; i < off + HexWidth; i++)
                text.Append(i < data.Length ? data[i].ToString("x2") + " " : "   ");
            text.Append(' ');
            for (int i = off; i < Math.Min(off + HexWidth, data.Length); i++)
                text.Append(data[i] >= 0x20 && data[i] < 0x7F ? (char)data[i] : '.');
            return text.ToString();
        }

        // Over the whole files; bytes past the end of the shorter one count as different.
        private static (long Count, long First) CountDifferences(Stream left, Stream right)
        {
            left.Position = right.Position = 0;
            var a = new byte[1 << 20];
            var b = new byte[1 << 20];
            long count = 0, first = -1, offset = 0;
            while (true)
            {
                int na = left.ReadAtLeast(a, a.Length, false), nb = right.ReadAtLeast(b, b.Length, false);
                int both = Math.Min(na, nb);
                for (int i = 0; i < both; i++)
                {
                    if (a[i] == b[i]) continue;
                    count++;
                    if (first < 0) first = offset + i;
                }
                if (na != nb)
                {
                    if (first < 0) first = offset + both;
                    count += Math.Abs(left.Length - right.Length);
                    return (count, first);
                }
                if (na == 0) return (count, first);
                offset += na;
            }
        }

        #endregion
    }
}
