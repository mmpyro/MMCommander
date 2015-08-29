using System.Windows;
using System.Windows.Input;

namespace Comander.View
{

    public partial class AboutWindow : Window
    {
        public AboutWindow(string message)
        {
            InitializeComponent();
            InformationTb.Text = message;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
