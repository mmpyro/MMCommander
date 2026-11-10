using Avalonia.Controls;
using Avalonia.Input;
using Comander.ViewModel;

namespace Comander.View
{
    public partial class TreeWindow : Window
    {
        private readonly TreeManager _treeManager;

        public TreeWindow(string path, IOManager manager)
        {
            InitializeComponent();
            _treeManager = new TreeManager(path, manager);
            DataContext = _treeManager;
        }

        private void TreeView_DoubleTapped(object sender, TappedEventArgs e)
        {
            if (Tree.SelectedItem is File file)
                _treeManager.SetItemChange(file);
        }
    }
}
