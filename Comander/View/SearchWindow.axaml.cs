using Comander.Core;
using Comander.View;
using LogLib;
using Search.View;
using Search.ViewModel;

namespace Search
{
    // Two pages in one window: search settings, then results (was a WPF Frame with page navigation).
    public partial class SearchWindow : DialogWindow
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
                // One press searches: the results page starts at once.
                searchVm.StartCommand.Execute(null);
            });
            searchPage.DataContext = searchVm;
            settingsPage.CloseButton.Click += (_, _) => Close();
            PageHost.Content = settingsPage;
            Opened += (_, _) => settingsPage.PhraseBox.Focus();
            // Don't leave a search running after the window is gone.
            Closed += (_, _) => searchVm.StopCommand.Execute(null);
        }
    }
}
