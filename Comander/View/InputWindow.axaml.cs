using Avalonia.Interactivity;

namespace Comander.View
{
    // ShowDialog<string>: the entered text, or null when cancelled.
    public partial class InputWindow : DialogWindow
    {
        public InputWindow(string textInfoMessage, string textField = "")
        {
            InitializeComponent();
            InfoTextBlock.Text = textInfoMessage;
            InputTextBox.Text = textField;
            Opened += (_, _) => { InputTextBox.Focus(); InputTextBox.SelectAll(); };
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Close(null);

        private void AcceptButton_OnClick(object sender, RoutedEventArgs e) => Close(InputTextBox.Text);
    }
}
