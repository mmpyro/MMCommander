using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Comander.Messages;
using Comander.ViewModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Comander.CommanderIO;
using Comander.Core;
using IOLib;

namespace Comander.Controls
{
    public partial class FilePane : UserControl
    {
        public static readonly StyledProperty<string> GridNameProperty =
            AvaloniaProperty.Register<FilePane, string>(nameof(GridName));

        public FilePane()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.Register<SetFocusMessage>(this, (_, msg) => SetFocusCallback(msg));
            DataGrid1.GotFocus += (_, _) => WeakReferenceMessenger.Default.Send(new FocusMessage { ManagerName = GridName });
            // DataGrid handles Enter/Tab/Home/Esc itself, so run our key bindings before it sees the key.
            DataGrid1.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            ThumbList.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            ThumbList.GotFocus += (_, _) => WeakReferenceMessenger.Default.Send(new FocusMessage { ManagerName = GridName });
            ThumbList.DoubleTapped += (_, _) => Execute(Panel?.EnterIntoDirCommand, null);
            DataGrid1.DoubleTapped += (_, _) => Execute(Panel?.EnterIntoDirCommand, null);
            DataGrid1.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
            // Row classes: "dir" for folders (Themes/Theme.axaml), "marked" for marked files. Rows are recycled, so set them on every load.
            DataGrid1.LoadingRow += (_, e) => TrackRow(e.Row);
            DataGrid1.UnloadingRow += (_, e) => UntrackRow(e.Row);
            // Clicking anywhere in the pane (path, filter, tabs) makes it the active pane.
            AddHandler(PointerPressedEvent, (_, _) => WeakReferenceMessenger.Default.Send(new FocusMessage { ManagerName = GridName }),
                RoutingStrategies.Tunnel, true);
            PathBar.PointerPressed += (_, e) => { if (e.Source is not TextBlock) BeginPathEdit(); };
            PathBox.LostFocus += (_, _) => EndPathEdit(false);
            PathBox.KeyDown += OnPathBoxKeyDown;
            DriveList.SelectionChanged += OnDrivePicked;
            // Settings that change how rows look need the columns and the listing refreshed.
            AppSettings.Current.PropertyChanged += (_, e) =>
            {
                ExtColumn().IsVisible = AppSettings.Current.SeparateExt;
                if (e.PropertyName is nameof(AppSettings.SeparateExt) or nameof(AppSettings.ColorIcons) or nameof(AppSettings.DateFormat) or nameof(AppSettings.SizeUnits))
                    Panel?.FilterFiles();
            };
            ExtColumn().IsVisible = AppSettings.Current.SeparateExt;
            Breadcrumb.LayoutUpdated += (_, _) => CrumbScroll.Offset = new Vector(Math.Max(0, CrumbScroll.Extent.Width - CrumbScroll.Viewport.Width), 0);
        }

        private DataGridColumn ExtColumn() => DataGrid1.Columns.First(c => Equals(c.Header, "EXT"));

        private readonly Dictionary<DataGridRow, (MetaData meta, PropertyChangedEventHandler handler)> _rows = new();

        private void TrackRow(DataGridRow row)
        {
            UntrackRow(row);
            var item = row.DataContext as IMetadataFileStructure;
            row.Classes.Set("dir", item?.IsDirectory == true);
            row.Classes.Set("marked", item?.IsSelected() == true);
            if (item?.MetaData == null) return;
            PropertyChangedEventHandler handler = (_, _) => row.Classes.Set("marked", item.IsSelected());
            item.MetaData.PropertyChanged += handler;
            _rows[row] = (item.MetaData, handler);
        }

        private void UntrackRow(DataGridRow row)
        {
            if (_rows.Remove(row, out var old))
                old.meta.PropertyChanged -= old.handler;
        }

        // Breadcrumb <-> path text box. Typing still lists each existing folder as you go.
        public void BeginPathEdit()
        {
            CrumbScroll.IsVisible = false;
            PathBox.IsVisible = true;
            PathBox.Focus();
            PathBox.SelectAll();
        }

        private void EndPathEdit(bool focusList)
        {
            if (!PathBox.IsVisible) return;
            PathBox.IsVisible = false;
            CrumbScroll.IsVisible = true;
            if (focusList) DataGrid1.Focus();
        }

        private void OnPathBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter or Key.Escape)
            {
                EndPathEdit(true);
                e.Handled = true;
            }
        }

        private void OnDrivePicked(object sender, SelectionChangedEventArgs e)
        {
            if (DriveList.SelectedItem is DriveStruct drive)
            {
                DriveButton.Flyout?.Hide();
                Panel?.NavigateCommand.Execute(drive.DriveLetter);
                DriveList.SelectedItem = null;
            }
        }

        public string GridName
        {
            get => GetValue(GridNameProperty);
            set => SetValue(GridNameProperty, value);
        }

        private IOManager Panel => DataContext as IOManager;

        // Panel keys from the central key map (Core/CommandRegistry.cs). Window-wide keys were already taken by MainWindow.
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = TopLevel.GetTopLevel(this)?.DataContext as MainVM;
            var command = vm?.KeyMap.Find(e);
            if (command == null)
                return;
            if (command.Id == CommandRegistry.EditPathId)
            {
                BeginPathEdit();
                e.Handled = true;
                return;
            }
            // Mark acts on the cursor item(s) of whichever view is showing.
            var parameter = command.Id != CommandRegistry.MarkId ? null
                : Panel?.IsThumbnails == true ? (System.Collections.IList)new System.Collections.ArrayList { ThumbList.SelectedItem } : DataGrid1.SelectedItems;
            if (Execute(command.Resolve(vm, Panel), parameter))
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
                    // A file from an older listing is not in the new list; selecting it would make the grid throw.
                    if (message.SelectedFile != null && items.Contains(message.SelectedFile))
                        DataGrid1.SelectedItem = message.SelectedFile;
                    else if (DataGrid1.SelectedItem == null || !items.Contains(DataGrid1.SelectedItem))
                        DataGrid1.SelectedItem = items[0];
                    DataGrid1.ScrollIntoView(DataGrid1.SelectedItem, null);
                    // Every new listing asks for focus; don't take it from a path or filter box the user is typing in.
                    if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not TextBox)
                    {
                        if (Panel?.IsThumbnails == true) ThumbList.Focus(); else DataGrid1.Focus();
                    }
                }
            });
        }
    }
}
