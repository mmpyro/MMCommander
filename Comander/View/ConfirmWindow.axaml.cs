using Avalonia.Interactivity;

namespace Comander.View
{
    // ShowDialog<bool>: true when accepted.
    public partial class ConfirmWindow : DialogWindow
    {
        public ConfirmWindow(string textMessage)
        {
            InitializeComponent();
            InfoTextBlock.Text = textMessage;
            Opened += (_, _) => AcceptButton.Focus();
        }

        private void AcceptButton_OnClick(object sender, RoutedEventArgs e) => Close(true);

        private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Close(false);
    }
}
