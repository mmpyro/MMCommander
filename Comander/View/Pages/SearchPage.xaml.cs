using System.Windows.Controls;
using Search.ViewModel;

namespace Search.View
{

    public partial class SearchPage : Page
    {
        public SearchPage()
        {
            InitializeComponent();
            SearchVm vm = this.DataContext as SearchVm;
            vm.Page = this;
        }
    }
}
