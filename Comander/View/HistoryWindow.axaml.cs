using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Comander.View
{
    // ShowDialog<string>: the chosen path, or null.
    public partial class HistoryWindow : Window
    {
        public HistoryWindow(IEnumerable<string> items)
        {
            InitializeComponent();
            ListView.ItemsSource = items;
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e) => Close(ListView.SelectedItem?.ToString());
    }
}
