using System.Windows;
using Comander.ViewModel;
using IOLib;

namespace Search
{
    public partial class SearchWindow : Window
    {
        public SearchWindow(IFileNotifier fileNotifier)
        {
            Locator.Notifier = fileNotifier;
            InitializeComponent();
        }

        public SearchWindow() : this(null)
        {
        }

    }
}
