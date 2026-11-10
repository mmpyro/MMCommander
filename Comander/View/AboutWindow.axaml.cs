using Avalonia.Interactivity;

namespace Comander.View
{
    public partial class AboutWindow : DialogWindow
    {
        public AboutWindow(string message)
        {
            InitializeComponent();
            InformationTb.Text = message;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
