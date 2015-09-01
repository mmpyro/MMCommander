using System.Collections.ObjectModel;
using System.Windows;
using Comander.Core;

namespace Comander.View
{

    public partial class ShortcutsWindow : Window
    {
        private readonly IShortcutManager _shortcutManager;
        private ObservableCollection<string> _items;

        public ShortcutsWindow(IShortcutManager shortcutManager) : this()
        {
            _shortcutManager = shortcutManager;
            Items = new ObservableCollection<string>(shortcutManager.GetShortcuts());
        }

        public ShortcutsWindow()
        {
            InitializeComponent();
        }

        public ObservableCollection<string> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                ListView.ItemsSource = value;
            }
        }

        public string Selected { get; set; }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            _shortcutManager.Remove(ListView.SelectedItem.ToString());
            Items = new ObservableCollection<string>(_shortcutManager.GetShortcuts());
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            Selected = ListView.SelectedItem.ToString();
            Close();
        }
    }
}
