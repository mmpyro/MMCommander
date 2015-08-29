using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Comander.Annotations;
using Comander.View;
using Comander.ViewModel.Commands;

namespace Comander.ViewModel
{
    public class HidenVM :INotifyPropertyChanged
    {

        public HidenVM(HidenWindowBase window)
        {
            DragCommand = new ExecuteCommand(window.DragMove);
            MouseEnterCommand = new ExecuteCommand(window.MouseEnter);
            MouseLeaveCommand = new ExecuteCommand(window.MouseLeave);
            CloseCommand = new ExecuteCommand(window.FinishTask);
        }

        #region ICommand
        public ICommand DragCommand { get; set; }
        public ICommand MouseEnterCommand { get; set; }
        public ICommand MouseLeaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}