using System.Linq;
using Avalonia.Controls;
using Comander.Converters;
using Comander.Dtos;
using IOLib;

namespace Comander.View
{
    // ShowDialog<CopyRequest>: null when cancelled.
    public partial class CopyDialog : DialogWindow
    {
        public CopyDialog(CopyDialogModel model)
        {
            InitializeComponent();
            var verb = model.IsMove ? "Move" : "Copy";
            var count = model.Names.Count;
            TitleText.Text = string.Format("{0} {1} item{2}", verb, count, count == 1 ? string.Empty : "s");
            var names = string.Join(", ", model.Names.Take(4)) + (count > 4 ? ", …" : string.Empty);
            SummaryText.Text = names + " · " + FileSizeConverter.Format(model.TotalSize);
            DestinationBox.Text = model.Destination;
            FreeText.Text = model.FreeSpaceText;
            PolicyList.SelectedIndex = (int)model.Policy;
            FilterBox.IsEnabled = model.HasFilter;
            OkText.Text = verb + "  ↵";
            HintText.Text = model.IsMove ? string.Empty : "Shift+F5 copies within this pane";
            CancelButton.Click += (_, _) => Close(null);
            OkButton.Click += (_, _) => Close(Request(true));
            QueueButton.Click += (_, _) => Close(Request(false));
            BrowseButton.Click += async (_, _) =>
            {
                var path = await WindowHost.PickFolder(DestinationBox.Text, this);
                if (path != null) DestinationBox.Text = path;
            };
            Opened += (_, _) => OkButton.Focus();
        }

        private CopyRequest Request(bool showProgress) => new CopyRequest
        {
            Destination = DestinationBox.Text?.Trim(),
            ShowProgress = showProgress,
            OnlyMatchingFilter = FilterBox.IsChecked == true,
            Options = new TransferOptions
            {
                Policy = (ConflictPolicy)System.Math.Max(0, PolicyList.SelectedIndex),
                PreserveTimestamps = PreserveBox.IsChecked == true,
                Verify = VerifyBox.IsChecked == true,
                FollowSymlinks = SymlinkBox.IsChecked == true,
            },
        };
    }
}
