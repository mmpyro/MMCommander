using Avalonia.Controls;
using Comander.Core;
using LogLib;
using Search.View;
using Search.ViewModel;

namespace Search
{
    // Two pages in one window: search settings, then results (was a WPF Frame with page navigation).
    public partial class SearchWindow : Window
    {
        public SearchWindow(IFileNotifier fileNotifier, ILogger logger)
        {
            InitializeComponent();
            var settingsPage = new SettingsPage();
            var searchPage = new SearchPage();
            var searchVm = new SearchVm(logger, fileNotifier, () => PageHost.Content = settingsPage);
            settingsPage.DataContext = new SettingsVm(fileNotifier is IIOManager panel ? panel.ActualPath : "", parameters =>
            {
                searchVm.Parameters = parameters;
                PageHost.Content = searchPage;
            });
            searchPage.DataContext = searchVm;
            PageHost.Content = settingsPage;
        }
    }
}
