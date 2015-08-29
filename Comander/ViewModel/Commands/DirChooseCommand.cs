using System;
using System.Windows.Input;
using WinForms = System.Windows.Forms; 

namespace Comander.ViewModel.Commands
{
    public class DirChooseCommand : ICommand
    {
        private IOManager _manager;

        public DirChooseCommand(IOManager manager)
        {
            _manager = manager;
        }

        public void Execute(object parameter)
        {
            WinForms.FolderBrowserDialog dialog = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = dialog.ShowDialog();
            if (result.Equals(WinForms.DialogResult.OK))
            {
                _manager.ActualPath = dialog.SelectedPath;
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}