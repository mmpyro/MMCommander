using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ZipLib;

namespace Comander.View
{
    

    public partial class ZipWindow : Window
    {
        private readonly string _directoryPath;
        public ZipParameters ZipParameters { get; set; }

        public ZipWindow(string directoryPath = "")
        {
            _directoryPath = directoryPath;
            InitializeComponent();
            ExecuteButton.IsEnabled = false;
            TypeBox.ItemsSource = Enum.GetNames(typeof (ZipType));
            TypeBox.SelectedIndex = 0;
        }

        private void Verify()
        {
            if (!string.IsNullOrEmpty(ArchivePathTextBox.Text) && !string.IsNullOrEmpty(ArchiveNameTextBox.Text))
            {
                ExecuteButton.IsEnabled = true;
            }
            else
            {
                ExecuteButton.IsEnabled = false;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (!string.IsNullOrEmpty(_directoryPath))
                dialog.SelectedPath = _directoryPath;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                ArchivePathTextBox.Text = dialog.SelectedPath;
            }
            Verify();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ZipType zipType = GetZipType((string)TypeBox.SelectedItem);
            if (!string.IsNullOrEmpty(PasswordBox.Password) && !string.IsNullOrEmpty(PasswordBox1.Password))
            {
                if (PasswordBox.Password.Equals(PasswordBox1.Password))
                {
                    ZipParameters = new ZipParameters(Path.Combine(ArchivePathTextBox.Text, ArchiveNameTextBox.Text),
                        PasswordBox.Password, zipType);
                    Close();
                }
                else
                {
                    StatusTextBlock.Text = "Passwords are diffrent";
                }
            }
            else
            {
                ZipParameters = new ZipParameters(Path.Combine(ArchivePathTextBox.Text, ArchiveNameTextBox.Text),
                    string.Empty, zipType);
                Close();
            }
        }

        private ZipType GetZipType(string selectedItem)
        {
            switch (selectedItem.ToLower())
            {
                case "zip":
                    return ZipType.Zip;
                case "tar":
                    return ZipType.Tar;
                case "gzip":
                    return ZipType.GZip;
                case "sevenzip":
                    return ZipType.SevenZip;
                case "bzip2":
                    return ZipType.Bzip2;
                default:
                    throw new ArgumentException("Unsupported exception");
            }
        }

        private void ArchiveNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Verify();
        }
    }
}
