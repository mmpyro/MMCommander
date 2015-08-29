using System.Windows.Controls;
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
