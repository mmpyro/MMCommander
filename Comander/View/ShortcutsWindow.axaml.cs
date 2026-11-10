using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Comander.Core;

namespace Comander.View
{
    // ShowDialog<string>: the chosen path, or null.
    public partial class ShortcutsWindow : DialogWindow
    {
        private readonly IShortcutManager _shortcutManager;

        public ShortcutsWindow(IShortcutManager shortcutManager)
        {
            InitializeComponent();
            _shortcutManager = shortcutManager;
            Reload();
        }

        private void Reload()
        {
            var list = (_shortcutManager.GetShortcuts() ?? Enumerable.Empty<string>()).ToList();
            ListView.ItemsSource = list;
            ListBorder.IsVisible = list.Count > 0;
            EmptyText.IsVisible = list.Count == 0;
            RemoveButton.IsEnabled = list.Count > 0;
            if (list.Count > 0) ListView.SelectedIndex = 0;
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListView.SelectedItem == null)
                return;
            _shortcutManager.Remove(ListView.SelectedItem.ToString());
            Reload();
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e) => Close(ListView.SelectedItem?.ToString());

        private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
