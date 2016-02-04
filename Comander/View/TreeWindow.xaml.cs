using Comander.ViewModel;
using System.Windows;


namespace Comander.View
{

    public partial class TreeWindow : Window
    {
        private readonly TreeManager _treeManager;
        private File _selectdFile;

        public TreeWindow(string path, IOManager manager)
        {
            InitializeComponent();
            _treeManager = new TreeManager(path, manager);
            stackPanel.DataContext = _treeManager;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var file = e.NewValue as File;
            if(file != null)
            {
                _selectdFile = file;
            }
        }

        private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(_selectdFile != null)
                _treeManager.SetItemChange(_selectdFile);
        }
    }
}
