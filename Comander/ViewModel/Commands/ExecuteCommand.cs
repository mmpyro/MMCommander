using System;
using System.Windows.Input;
using LogLib;

namespace Comander.ViewModel.Commands
{
    public class ExecuteCommand : ICommand
    {
        private readonly ILogger _logger;
        private readonly Action _action;

        public ExecuteCommand(Action action)
        {
            _action = action;
        }

        public ExecuteCommand(Action action, ILogger logger) : this(action)
        {
            _logger = logger;
        }

        public void Execute(object parameter)
        {
            try
            {
                _action();
            }
            catch(Exception ex)
            {
                if (_logger != null)
                    _logger.WriteLine(ex,LogInfo.Error);
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
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