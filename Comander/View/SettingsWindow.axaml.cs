using System.IO;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Comander.ViewModel;

namespace Comander.View
{
    // Done saves; Cancel, Esc or closing the window undoes the live changes.
    public partial class SettingsWindow : DialogWindow
    {
        private readonly SettingsWindowVM _vm;
        private bool _saved;

        public SettingsWindow(SettingsWindowVM vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            DoneButton.Click += (_, _) =>
            {
                _vm.Save();
                _saved = true;
                Close();
            };
            CancelButton.Click += (_, _) => Close();
            Closing += (_, _) => { if (!_saved) _vm.Revert(); };
            ExportButton.Click += async (_, _) => await Export();
            // While a shortcut records, every key goes to it (Tab, Space, Esc included).
            AddHandler(KeyDownEvent, (_, e) =>
            {
                if (_vm.Recording != null && _vm.RecordKey(e.Key, e.KeyModifiers))
                    e.Handled = true;
            }, RoutingStrategies.Tunnel);
        }

        private async System.Threading.Tasks.Task Export()
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export key bindings",
                SuggestedFileName = "mmcommander-keys.xml",
            });
            if (file == null) return;
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_vm.ExportText());
        }
    }
}
