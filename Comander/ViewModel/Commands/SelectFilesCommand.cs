using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Comander.CommanderIO;

namespace Comander.ViewModel.Commands
{
    public class SelectFilesCommand : ICommand
    {
        private Action _action;

        public SelectFilesCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            var fileList = Cast(parameter);
            if (fileList != null)
            {
                return true;
            }
            return false;
        }

        public void Execute(object parameter)
        {
            Cast(parameter).ForEach(t => t.SelectFile());
            _action();
        }

        protected List<IMetadataFileStructure> Cast(object parameter)
        {
            var list = parameter as IList;
            return list.Cast<IMetadataFileStructure>().ToList();
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