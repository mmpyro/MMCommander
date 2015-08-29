using System.Collections.Generic;
using System.Windows;

namespace Comander.View
{

    public partial class HistoryWindow : Window
    {
        private IEnumerable<string> _items;


        public HistoryWindow()
        {
            InitializeComponent();
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            Selected = ListView.SelectedItem.ToString();
            Close();
        }

        public IEnumerable<string> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                ListView.ItemsSource = value;
            }
        }

        public string Selected { get; set; }

    }
}
