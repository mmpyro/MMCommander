using System;
using System.Collections.Generic;
using System.Windows.Input;
using Comander.Core;
using IOLib;
using LogLib;

namespace Comander.ViewModel.Commands
{
    public class PluginCommand : ICommand
    {
        private readonly IPluginManager _pluginManager;
        private readonly IEnumerable<IAbstractFileStructure> _files;
        private readonly ILogger _logger;

        public PluginCommand(IPluginManager pluginManager, IEnumerable<IAbstractFileStructure> files, ILogger logger )
        {
            _pluginManager = pluginManager;
            _files = files;
            _logger = logger;
        }

        public bool CanExecute(object parameter)
        {
            return !string.IsNullOrEmpty(parameter.ToString());
        }

        public void Execute(object parameter)
        {
            try
            {
                _pluginManager.InvokeMethod(parameter.ToString(), _files);
            }
            catch (Exception e)
            {
                _logger.WriteLine(e, LogInfo.Error);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }
}