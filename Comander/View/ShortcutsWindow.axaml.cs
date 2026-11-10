using Avalonia.Controls;
using Avalonia.Interactivity;
using Comander.Core;

namespace Comander.View
{
    // ShowDialog<string>: the chosen path, or null.
    public partial class ShortcutsWindow : Window
    {
        private readonly IShortcutManager _shortcutManager;

        public ShortcutsWindow(IShortcutManager shortcutManager)
        {
            InitializeComponent();
            _shortcutManager = shortcutManager;
            ListView.ItemsSource = shortcutManager.GetShortcuts();
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListView.SelectedItem == null)
                return;
            _shortcutManager.Remove(ListView.SelectedItem.ToString());
            ListView.ItemsSource = _shortcutManager.GetShortcuts();
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e) => Close(ListView.SelectedItem?.ToString());
    }
}
