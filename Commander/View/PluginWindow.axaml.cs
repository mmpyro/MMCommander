using System.Collections.Generic;
using Avalonia.Interactivity;
using Commander.Core;
using Commander.ViewModel;
using LogLib;

namespace Commander.View
{
    public partial class PluginWindow : HidenWindowBase
    {
        public PluginWindow(IPluginManager pluginManager, string dir, IEnumerable<string> files, ILogger logger)
        {
            InitializeComponent();
            var vm = new PluginVM(pluginManager, dir, files, logger);
            DataContext = vm;
            var any = vm.PluginNames.Count > 0;
            ListBorder.IsVisible = any;
            EmptyText.IsVisible = !any;
            RunButton.IsEnabled = any;
            if (any)
                PluginListView.SelectedIndex = 0;
            Opened += (_, _) => PluginListView.Focus();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
