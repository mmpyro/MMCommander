using System;
using System.IO;
using System.Windows.Input;

namespace Comander.ViewModel.Commands
{
    public class ParentDirCommand : ICommand
    {
        private IOManager _manager;

        public ParentDirCommand(IOManager manager)
        {
            _manager = manager;
        }

        public void Execute(object parameter)
        {
            DirectoryInfo dir = Directory.GetParent(_manager.ActualPath);
            if (dir != null)
            {
                _manager.ActualPath = dir.FullName;
            }
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