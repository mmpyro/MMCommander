using System.Collections.Generic;
using Comander.Core;
using Comander.ViewModel;
using LogLib;

namespace Comander.View
{
    public partial class PluginWindow : HidenWindowBase
    {
        public PluginWindow(IPluginManager pluginManager, string dir, IEnumerable<string> files, ILogger logger)
        {
            InitializeComponent();
            DataContext = new PluginVM(pluginManager, dir, files, logger);
            Opened += (_, _) => PluginListView.Focus();
        }
    }
}
