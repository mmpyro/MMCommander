using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
            UnitComboBox.SelectedIndex = 2;
        }

        private async void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            PathTextBox.Text = await WindowHost.PickFolder(PathTextBox.Text, this) ?? PathTextBox.Text;
        }

        // Split runs on the thread pool so a multi-GB file doesn't freeze the UI (BUG-23).
        private async void ExecuteButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ExecuteButton.IsEnabled = false;
                StatusBlock.Text = "Split in progress ...";
                var fileFactory = new FileFactory();
                var fileManager = new FileManager(fileFactory);
                var unit = (FileSizeUnit) UnitComboBox.SelectedItem;
                string source = SplitPathTextBox.Text, destination = PathTextBox.Text;
                int partSize = _fileSize;
                await Task.Run(() => fileManager.Split(fileFactory.CreateFileMsg(source), partSize, unit, destination));
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

        private async void BrowseButton1_OnClick(object sender, RoutedEventArgs e)
        {
            SplitPathTextBox.Text = await WindowHost.PickFile(this) ?? SplitPathTextBox.Text;
            ShowFileSize(_fileSize);
        }

        private void PartSize_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(NumericTextBox.Text, out var fileSize) || fileSize <= 0)
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
                PartsTextBlock.Text = "Parts: " + (fileSize/(partSize*(long)unit));
            }
            catch
            {
                PartsTextBlock.Text = string.Empty;
            }
        }

        private void ShowFileSize(int partSize)
        {
            try
            {
                if (!string.IsNullOrEmpty(SplitPathTextBox.Text))
                {
                    FileInfo file = new FileInfo(SplitPathTextBox.Text);
                    FileSizeTextBlock.Text = file.Length + " B";
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
            if (FileSizeTextBlock != null)
                ShowFileSize(_fileSize);
        }
    }
}
