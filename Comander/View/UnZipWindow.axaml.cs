using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Comander.CommanderIO;
using Comander.Core;
using ZipLib;

namespace Comander.View
{
    // ShowDialog<UnZipRequest>: null when closed without executing. The archive format is detected on extraction.
    public partial class UnZipWindow : Window
    {
        private readonly IMetadataFileStructure _file;

        public UnZipWindow(IMetadataFileStructure file)
        {
            _file = file;
            InitializeComponent();
            ExtractDirRadio.Content = "Extract to " + Path.GetFileNameWithoutExtension(file.Name);
        }

        // Single password box: extraction never needs a confirmation (BUG-05).
        private void Execute_OnClick(object sender, RoutedEventArgs e) =>
            Close(new UnZipRequest
            {
                Parameters = new ZipParameters(_file.FullName, PasswordBox.Text ?? string.Empty),
                ExtractType = ExtractDirRadio.IsChecked == true ? ExtractType.ToSpecificDir : ExtractType.Here
            });
    }
}
