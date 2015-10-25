using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using IOLib;
using LogLib;


namespace Comander.View
{

    public partial class SplitWindow : Window
    {
        private readonly ILogger _logger;
        private int _fileSize = 0;

        public SplitWindow(ILogger logger)
        {
            InitializeComponent();
            _logger = logger;
            UnitComboBox.ItemsSource = new List<FileSizeUnit>() {FileSizeUnit.B, FileSizeUnit.KB, FileSizeUnit.MB, FileSizeUnit.GB};
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                PathTextBox.Text = dialog.SelectedPath;
            }
        }


        private void ExecuteButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusBlock.Text = "Split in progress ...";
                var fileFactory = new FileFactory();
                var fileManager = new FileManager(fileFactory);
                FileSizeUnit unit = (FileSizeUnit) UnitComboBox.SelectedItem;
                fileManager.Split(fileFactory.CreateFileMsg(SplitPathTextBox.Text), _fileSize, unit,
                    PathTextBox.Text);
                _logger.Info("Split was finished without errors.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                Close();
            }
        }

        private void BrowseButton1_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                SplitPathTextBox.Text = dialog.FileName;
            }
        }

        private void SplitPathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int fileSize;
            if (!int.TryParse(textBox.Text, out fileSize))
            {
                ExecuteButton.IsEnabled = false;
            }
            else
            {
                ExecuteButton.IsEnabled = true;
                _fileSize = fileSize;
                ShowFileSize(fileSize);
            }
        }

        private void CalculateNumbersOfParts(long fileSize, int partSize)
        {
            try
            {
                FileSizeUnit unit = (FileSizeUnit)UnitComboBox.SelectedItem;
                PartsTextBlock.Text = (fileSize/(partSize*(long)unit)).ToString();
            }
            catch
            {
                PartsTextBlock.Text = string.Empty;
            }
        }

        /// <summary>
        /// Show file size in bytes
        /// </summary>
        private void ShowFileSize(int partSize)
        {
            try
            {
                if (!string.IsNullOrEmpty(SplitPathTextBox.Text))
                {
                    FileInfo file = new FileInfo(SplitPathTextBox.Text);
                    FileSizeTextBlock.Text = file.Length.ToString();
                    CalculateNumbersOfParts(file.Length, partSize);
                }
            }
            catch
            {
                FileSizeTextBlock.Text = string.Empty;
            }
        }

        private void UnitComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowFileSize(_fileSize);
        }
    }
}
