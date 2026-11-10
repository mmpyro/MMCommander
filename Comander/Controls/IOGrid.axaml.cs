using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Comander.Messages;
using Comander.ViewModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Linq;

namespace Comander.Controls
{
    public partial class IOGrid : UserControl
    {
        public static readonly StyledProperty<string> GridNameProperty =
            AvaloniaProperty.Register<IOGrid, string>(nameof(GridName));

        public IOGrid()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.Register<SetFocusMessage>(this, (_, msg) => SetFocusCallback(msg));
            DataGrid1.GotFocus += (_, _) => WeakReferenceMessenger.Default.Send(new FocusMessage { ManagerName = GridName });
            // DataGrid handles Enter/Tab/Home/Esc itself, so run our key bindings before it sees the key.
            DataGrid1.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            DataGrid1.DoubleTapped += (_, _) => Execute(Panel?.EnterIntoDirCommand, null);
            DataGrid1.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
        }

        public string GridName
        {
            get => GetValue(GridNameProperty);
            set => SetValue(GridNameProperty, value);
        }

        private IOManager Panel => DataContext as IOManager;

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var binding = DataGrid1.KeyBindings.FirstOrDefault(b => b.Gesture != null && b.Gesture.Matches(e));
            if (binding != null && Execute(binding.Command, binding.CommandParameter))
                e.Handled = true;
        }

        // Right double click toggles selection of the clicked rows.
        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.ClickCount == 2 && e.GetCurrentPoint(DataGrid1).Properties.IsRightButtonPressed)
                Execute(Panel?.SelectCommand, DataGrid1.SelectedItems);
        }

        private static bool Execute(System.Windows.Input.ICommand command, object parameter)
        {
            if (command == null || !command.CanExecute(parameter))
                return false;
            command.Execute(parameter);
            return true;
        }

        private void SetFocusCallback(SetFocusMessage message)
        {
            if (!string.Equals(message.GridName, GridName, StringComparison.OrdinalIgnoreCase))
                return;
            Dispatcher.UIThread.Post(() =>
            {
                if (DataGrid1.ItemsSource is System.Collections.IList items && items.Count > 0)
                {
                    DataGrid1.SelectedItem ??= message.SelectedFile ?? items[0];
                    DataGrid1.ScrollIntoView(DataGrid1.SelectedItem, null);
                    DataGrid1.Focus();
                }
            });
        }
    }
}
