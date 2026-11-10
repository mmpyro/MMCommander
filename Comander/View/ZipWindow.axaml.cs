using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ZipLib;

namespace Comander.View
{
    // ShowDialog<ZipParameters>: null when closed without executing.
    public partial class ZipWindow : DialogWindow
    {
        public ZipWindow(string directoryPath = "")
        {
            InitializeComponent();
            ArchivePathTextBox.Text = directoryPath;
            TypeBox.ItemsSource = Enum.GetValues<ZipType>();
            TypeBox.SelectedIndex = 0;
        }

        // A password typed in either box must be typed in both and match; it's never dropped silently (BUG-05).
        public static string ValidatePasswords(string password, string confirmation, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(password) && string.IsNullOrEmpty(confirmation))
                return string.Empty;
            if (password != confirmation)
                error = string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmation)
                    ? "Type the password in both boxes"
                    : "Passwords are different";
            return error == null ? password : null;
        }

        private void Verify()
        {
            ExecuteButton.IsEnabled = !string.IsNullOrEmpty(ArchivePathTextBox.Text) && !string.IsNullOrEmpty(ArchiveNameTextBox.Text);
        }

        private async void Browse_OnClick(object sender, RoutedEventArgs e)
        {
            ArchivePathTextBox.Text = await WindowHost.PickFolder(ArchivePathTextBox.Text, this) ?? ArchivePathTextBox.Text;
            Verify();
        }

        private void Execute_OnClick(object sender, RoutedEventArgs e)
        {
            var zipType = (ZipType)TypeBox.SelectedItem;
            var password = ValidatePasswords(PasswordBox.Text, PasswordBox1.Text, out var error);
            if (error == null && !string.IsNullOrEmpty(password) && zipType != ZipType.Zip)
                error = "Passwords are supported only for Zip";
            if (error != null)
            {
                StatusTextBlock.Text = error;
                return;
            }
            Close(new ZipParameters(Path.Combine(ArchivePathTextBox.Text, ArchiveNameTextBox.Text), password, zipType));
        }

        private void ArchiveNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => Verify();

        private void Cancel_OnClick(object sender, RoutedEventArgs e) => Close();
    }
}
