using System;
using System.Windows.Input;

namespace Comander.ViewModel.Commands
{
    public class RelayCommand : ICommand
    {
          private readonly Action<object> _execute;
        private readonly Action<Exception> _logAction;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Action<Exception> logAction  ,Predicate<object> canExecute)
        {
            _execute = execute;
            _logAction = logAction;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute)
        {
            _execute = execute;
            _logAction = null;
            _canExecute = _ => true;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            try
            {
                _execute(parameter);
            }
            catch (Exception e)
            {
                if(_logAction != null)
                    _logAction(e);
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}