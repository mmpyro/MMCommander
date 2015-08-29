using System.Windows;
using System.Windows.Controls;


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

    public partial class JoinWindow : Window
    {
        public JoinFileDTO JoinParameter { get; set; }
        public JoinWindow()
        {
            InitializeComponent();
            ExecuteButton.IsEnabled = false;
        }

        private void Verify()
        {
            if (!string.IsNullOrEmpty(JoinPathTextBox.Text) && !string.IsNullOrEmpty(PathTextBox.Text)
                && !string.IsNullOrEmpty(FileNameTextBox.Text))
            {
                ExecuteButton.IsEnabled = true;
            }
            else
            {
                ExecuteButton.IsEnabled = false;
            }
        }

        private void BrowseButton1_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                JoinPathTextBox.Text = dialog.FileName;
            }
            Verify();
        }

        private void BrowseButton2_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                PathTextBox.Text = dialog.SelectedPath;
            }
            Verify();
        }

        private void ExecuteButton_OnClick(object sender, RoutedEventArgs e)
        {
            JoinParameter = new JoinFileDTO(JoinPathTextBox.Text, PathTextBox.Text, FileNameTextBox.Text);
            Close();
        }

        private void FileNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Verify();
        }
    }
}
