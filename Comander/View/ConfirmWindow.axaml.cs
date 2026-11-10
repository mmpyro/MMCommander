using Avalonia.Interactivity;
using Comander.Converters;

namespace Comander.View
{
    public enum WindowIcon
    {
        Info,
        Warrning,
        Error
    }

    // ShowDialog<bool>: true when accepted.
    public partial class ConfirmWindow : DialogWindow
    {
        public ConfirmWindow(string textMessage, WindowIcon icon = WindowIcon.Warrning)
        {
            InitializeComponent();
            InfoTextBlock.Text = textMessage;
            Image.Source = IconFor(icon);
            Opened += (_, _) => AcceptButton.Focus();
        }

        public static Avalonia.Media.Imaging.Bitmap IconFor(WindowIcon icon) =>
            Icons.Load(icon == WindowIcon.Warrning ? "Alert-48.png" : icon == WindowIcon.Error ? "Cancel-48.png" : "info_black.png");

        private void AcceptButton_OnClick(object sender, RoutedEventArgs e) => Close(true);

        private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Close(false);
    }
}
