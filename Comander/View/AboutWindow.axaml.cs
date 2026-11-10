using Avalonia.Interactivity;

namespace Comander.View
{
    public partial class AboutWindow : DialogWindow
    {
        public AboutWindow(string version)
        {
            InitializeComponent();
            VersionText.Text = "Version " + version;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
