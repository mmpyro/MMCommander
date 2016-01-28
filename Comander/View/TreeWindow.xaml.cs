using Comander.ViewModel;
using System.Windows;


namespace Comander.View
{

    public partial class TreeWindow : Window
    {
        public TreeWindow(string path)
        {
            InitializeComponent();
            stackPanel.DataContext = new TreeManager(path);
        }
    }
}
