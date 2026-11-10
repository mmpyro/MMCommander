using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Comander.ViewModel;

namespace Comander.View
{
    public partial class TreeWindow : DialogWindow
    {
        private readonly TreeManager _treeManager;

        public TreeWindow(string path, IOManager manager)
        {
            InitializeComponent();
            _treeManager = new TreeManager(path, manager);
            DataContext = _treeManager;
            PathText.Text = path;
            ToolTip.SetTip(PathText, path);
            // TreeViewItem takes Enter itself, so IsDefault never fires; catch it before the tree does.
            AddHandler(KeyDownEvent, (_, e) =>
            {
                if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
                {
                    Open();
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
            // Start with the current folder open and selected, so its children show and Enter works at once.
            Opened += (_, _) =>
            {
                if (Tree.ContainerFromIndex(0) is TreeViewItem root)
                {
                    root.IsExpanded = true;
                    Tree.SelectedItem = Tree.Items[0];
                    root.Focus();
                }
            };
        }

        private void Open()
        {
            if (Tree.SelectedItem is File file)
                _treeManager.SetItemChange(file);
        }

        private void TreeView_DoubleTapped(object sender, TappedEventArgs e) => Open();

        private void GoButton_OnClick(object sender, RoutedEventArgs e) => Open();

        private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
