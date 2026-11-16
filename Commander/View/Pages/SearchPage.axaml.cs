using Avalonia.Controls;
using Avalonia.Input;
using Search.ViewModel;

namespace Search.View
{
    public partial class SearchPage : UserControl
    {
        public SearchPage()
        {
            InitializeComponent();
        }

        private void Select_DoubleTapped(object sender, TappedEventArgs e) =>
            (DataContext as SearchVm)?.SelectCommand.Execute(null);
    }
}
