using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Comander.View
{
    public class JoinFileDTO
    {
        public string JoinPath { get; set; }
        public string DestinationPath { get; set; }
        public string FileName { get; set; }

        public JoinFileDTO(string joinPath, string destinationPath, string fileName)
        {
            JoinPath = joinPath;
            DestinationPath = destinationPath;
            FileName = fileName;
        }
    }

    // ShowDialog<JoinFileDTO>: null when closed without executing.
    public partial class JoinWindow : Window
    {
        public JoinWindow()
        {
            InitializeComponent();
        }

        private void Verify()
        {
            ExecuteButton.IsEnabled = !string.IsNullOrEmpty(JoinPathTextBox.Text) && !string.IsNullOrEmpty(PathTextBox.Text)
                && !string.IsNullOrEmpty(FileNameTextBox.Text);
        }

        private async void BrowseButton1_OnClick(object sender, RoutedEventArgs e)
        {
            JoinPathTextBox.Text = await WindowHost.PickFile(this) ?? JoinPathTextBox.Text;
            Verify();
        }

        private async void BrowseButton2_OnClick(object sender, RoutedEventArgs e)
        {
            PathTextBox.Text = await WindowHost.PickFolder(PathTextBox.Text, this) ?? PathTextBox.Text;
            Verify();
        }

        private void ExecuteButton_OnClick(object sender, RoutedEventArgs e) =>
            Close(new JoinFileDTO(JoinPathTextBox.Text, PathTextBox.Text, FileNameTextBox.Text));

        private void FileNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => Verify();
    }
}
