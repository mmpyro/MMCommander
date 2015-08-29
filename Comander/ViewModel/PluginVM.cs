using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Comander.Core;
using Comander.View;
using Comander.ViewModel.Commands;
using IOLib;
using LogLib;

namespace Comander.ViewModel
{
    public class PluginVM : HidenVM
    {
        private ObservableCollection<string> _pluginNames; 

        public PluginVM(HidenWindowBase window, IPluginManager pluginManager, IEnumerable<IAbstractFileStructure> files, ILogger logger) : base(window)
        {
            PluginInvokeCommand = new PluginCommand(pluginManager, files, logger);
            PluginNames = new ObservableCollection<string>(pluginManager.GetMethods());
        }

        public ObservableCollection<string> PluginNames
        {
            get { return _pluginNames; }
            set
            {
                if (Equals(value, _pluginNames)) return;
                _pluginNames = value;
                OnPropertyChanged();
            }
        }

        public ICommand PluginInvokeCommand { get; set; }
    }
}