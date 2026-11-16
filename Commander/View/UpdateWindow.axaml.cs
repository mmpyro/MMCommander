using Avalonia.Interactivity;
using Commander.Core;

namespace Commander.View
{
    // ShowDialog<UpdateAnswer>: Later (also Esc), Skip or Download.
    public partial class UpdateWindow : DialogWindow
    {
        public UpdateWindow(UpdateModel model)
        {
            InitializeComponent();
            VersionText.Text = string.Format("MMCommander {0} is out (you have {1}).", model.Release.Tag.TrimStart('v'), model.Current);
            NotesBox.Text = string.IsNullOrWhiteSpace(model.Release.Notes) ? "No release notes." : model.Release.Notes.Trim();
            DownloadButton.Content = model.CanInstall ? "Update and restart" : "Open download page";
            Opened += (_, _) => DownloadButton.Focus();
        }

        private void Skip_OnClick(object sender, RoutedEventArgs e) => Close(UpdateAnswer.Skip);

        private void Later_OnClick(object sender, RoutedEventArgs e) => Close(UpdateAnswer.Later);

        private void Download_OnClick(object sender, RoutedEventArgs e) => Close(UpdateAnswer.Download);
    }
}
