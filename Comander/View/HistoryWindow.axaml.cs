using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Comander.View
{
    // ShowDialog<string>: the chosen path, or null.
    public partial class HistoryWindow : DialogWindow
    {
        public HistoryWindow(IEnumerable<string> items)
        {
            InitializeComponent();
            var list = items?.ToList() ?? new List<string>();
            ListView.ItemsSource = list;
            ListBorder.IsVisible = list.Count > 0;
            EmptyText.IsVisible = list.Count == 0;
            if (list.Count > 0) ListView.SelectedIndex = 0;
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e) => Close(ListView.SelectedItem?.ToString());

        private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
