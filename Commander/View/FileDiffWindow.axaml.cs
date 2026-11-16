using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Commander.ViewModel;

namespace Commander.View
{
    public partial class FileDiffWindow : DialogWindow
    {
        private const double ScrollStep = 60;
        private readonly FileDiffVM _vm;
        private bool _discardConfirmed;

        public FileDiffWindow(FileDiffVM vm)
        {
            InitializeComponent();
            CanResize = true;
            DataContext = _vm = vm;
            CloseButton.Click += (_, _) => Close();
            // Tunnel: the list and the edit box would eat Enter, Tab, arrows and Escape first.
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            RowList.AddHandler(PointerPressedEvent, OnRowPressed, RoutingStrategies.Tunnel);
            RowList.DoubleTapped += (_, _) => _vm.BeginEdit();
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FileDiffVM.IsEditing))
                    Dispatcher.UIThread.Post(vm.IsEditing ? FocusEditor : FocusList, DispatcherPriority.Loaded);
            };
            Opened += async (_, _) =>
            {
                await vm.Load();
                FocusList();
            };
            Closing += OnClosing;
        }

        private void FocusEditor()
        {
            var box = RowList.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.IsEffectivelyVisible);
            if (box == null) return;
            box.Focus();
            box.CaretIndex = _vm.EditCaret;
        }

        private void FocusList()
        {
            if (_vm.IsEditing) return;
            (RowList.ContainerFromIndex(_vm.CursorIndex) ?? (Control)RowList).Focus();
        }

        // A click picks the side (left or right half) before the list moves the cursor there.
        private void OnRowPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.Source is Control c && c.FindAncestorOfType<TextBox>(true) != null) return;
            _vm.CommitEdit();
            _vm.ActiveSide = e.GetPosition(RowList).X < RowList.Bounds.Width / 2 ? 0 : 1;
        }

        private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            bool alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            bool editing = _vm.IsEditing;
            var box = e.Source as TextBox;
            e.Handled = true;
            switch (e.Key)
            {
                case Key.S when ctrl:
                    await _vm.Save();
                    break;
                case Key.Z when ctrl:
                    if (editing) _vm.CancelEdit();
                    else _vm.Undo();
                    break;
                case Key.Y when ctrl:
                    _vm.DeleteLine();
                    break;
                case Key.F7:
                    _vm.CommitEdit();
                    if (shift) _vm.PrevDiff();
                    else _vm.NextDiff();
                    break;
                case Key.F5:
                    await _vm.Reload();
                    break;
                case Key.Right when alt:
                    _vm.CopyBlock(1);
                    break;
                case Key.Left when alt:
                    _vm.CopyBlock(0);
                    break;
                case Key.Tab:
                    _vm.CommitEdit();
                    _vm.ActiveSide = 1 - _vm.ActiveSide;
                    break;
                case Key.Escape when editing:
                    _vm.CommitEdit();
                    break;
                case Key.Enter when editing:
                    _vm.SplitLine(box?.CaretIndex ?? int.MaxValue);
                    break;
                case Key.Back when editing && box != null && box.CaretIndex == 0 && box.SelectionStart == box.SelectionEnd:
                    _vm.JoinLine();
                    break;
                case Key.Up or Key.Down when editing:
                    int caret = box?.CaretIndex ?? 0;
                    _vm.CommitEdit();
                    _vm.Move(e.Key == Key.Up ? -1 : 1);
                    _vm.BeginEdit(caret);
                    break;
                case Key.Enter or Key.F2 when !editing:
                    _vm.BeginEdit();
                    break;
                case Key.Left when !editing:
                    _vm.HOffset -= ScrollStep;
                    break;
                case Key.Right when !editing:
                    _vm.HOffset += ScrollStep;
                    break;
                default:
                    e.Handled = false;
                    return;
            }
            if (!_vm.IsEditing)
                Dispatcher.UIThread.Post(FocusList, DispatcherPriority.Loaded);
        }

        private async void OnClosing(object sender, WindowClosingEventArgs e)
        {
            _vm.CommitEdit();
            if (_discardConfirmed || !_vm.HasChanges) return;
            e.Cancel = true;
            if (await _vm.ConfirmDiscard())
            {
                _discardConfirmed = true;
                Close();
            }
        }
    }
}
