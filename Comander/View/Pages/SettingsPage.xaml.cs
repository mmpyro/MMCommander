using System.Windows.Controls;
using Comander.Messages;
using IOLib;
using Search.ViewModel;

namespace Search.View
{

    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            SettingsVm vm = this.DataContext as SettingsVm;
            vm.Page = this;
        }
    }
}
