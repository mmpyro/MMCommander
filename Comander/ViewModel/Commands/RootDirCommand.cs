using System;
using System.IO;
using System.Windows.Input;

namespace Comander.ViewModel.Commands
{
    public class RootDirCommand : ICommand
    {
        private IOManager _manager;
        

        public RootDirCommand(IOManager manager)
        {
            _manager = manager;
        }

        public void Execute(object parameter)
        {
            _manager.ActualPath = Directory.GetDirectoryRoot(_manager.ActualPath);
        }

        public bool CanExecute(object parameter)
        {
            if (Directory.Exists(_manager.ActualPath))
                return true;
            return false;
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