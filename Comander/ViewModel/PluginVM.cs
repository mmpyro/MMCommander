using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Comander.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogLib;

namespace Comander.ViewModel
{
    public class PluginVM : ObservableObject
    {
        public PluginVM(IPluginManager pluginManager, string dir, IEnumerable<string> files, ILogger logger)
        {
            PluginNames = new ObservableCollection<string>(pluginManager.GetMethods());
            // Awaited, so a plugin that fails asynchronously is still logged (BUG-25).
            PluginInvokeCommand = new AsyncRelayCommand<string>(async name =>
            {
                if (string.IsNullOrEmpty(name))
                    return;
                try
                {
                    await pluginManager.InvokeMethod(name, dir, files);
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            });
        }

        public ObservableCollection<string> PluginNames { get; }

        public ICommand PluginInvokeCommand { get; }
    }
}
