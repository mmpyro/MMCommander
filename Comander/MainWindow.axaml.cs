using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Comander.Core;
using Comander.Messages;
using Comander.ViewModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Comander
{
    public partial class MainWindow : Window
    {
        private PaletteVM _palette;
        private int _dimmed;

        // The most recently created main window (dialog owner when there is no desktop lifetime, e.g. headless tests).
        public static MainWindow Last { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Last = this;
            // Newest message is last; show it when the log panel opens.
            LogsButton.Flyout.Opened += (_, _) =>
            {
                Vm?.MarkLogsSeen();
                if (LogList.ItemCount > 0)
                    LogList.ScrollIntoView(LogList.ItemCount - 1);
            };
            // Window-wide keys (Ctrl+K, ...) work from anywhere, before a panel or text box sees them.
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            PaletteBox.KeyDown += OnPaletteKey;
            PaletteList.Tapped += (_, _) => _palette?.RunCommand.Execute(null);
            PaletteOverlay.PointerPressed += (_, e) => { if (e.Source == PaletteOverlay) _palette?.Close(); };
            DataContextChanged += (_, _) => WatchPalette();
        }

        private MainVM Vm => DataContext as MainVM;

        // Counted, so a dialog opened from a dialog keeps the dim until both close.
        public void Dim(bool on)
        {
            _dimmed = Math.Max(0, _dimmed + (on ? 1 : -1));
            DimOverlay.IsVisible = _dimmed > 0;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = Vm;
            var command = vm?.KeyMap.Find(e, c => c.Scope == CommandScope.App);
            var icommand = command?.Resolve(vm, vm.ActivePanel);
            if (icommand != null && icommand.CanExecute(null))
            {
                icommand.Execute(null);
                e.Handled = true;
            }
        }

        private void WatchPalette()
        {
            if (_palette != null) _palette.PropertyChanged -= OnPaletteChanged;
            _palette = Vm?.Palette;
            if (_palette != null) _palette.PropertyChanged += OnPaletteChanged;
        }

        private void OnPaletteChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaletteVM.IsOpen) && _palette.IsOpen)
                Avalonia.Threading.Dispatcher.UIThread.Post(() => PaletteBox.Focus());
            if (e.PropertyName == nameof(PaletteVM.Selected) && _palette.Selected != null)
                PaletteList.ScrollIntoView(_palette.Selected);
        }

        private void OnPaletteKey(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down: _palette.Move(1); break;
                case Key.Up: _palette.Move(-1); break;
                case Key.Enter: _palette.RunCommand.Execute(null); break;
                case Key.Escape: _palette.Close(); break;
                default: return;
            }
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new WindowCloseEventArgs());
            base.OnClosed(e);
        }
    }
}
