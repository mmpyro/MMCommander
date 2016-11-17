
using System.Collections.Generic;
using Comander.Core;
using Comander.ViewModel;
using IOLib;
using LogLib;

namespace Comander.View
{

    public partial class PluginWindow : HidenWindowBase
    {
        public PluginWindow(IPluginManager pluginManager, string dir, IEnumerable<string> files, ILogger logger)
        {
            InitializeComponent();
            InitTask();
            DataContext = new PluginVM(this, pluginManager, dir, files, logger);
        }
    }
}
