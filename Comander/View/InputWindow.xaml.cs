using System.Windows;
using System.Windows.Input;


namespace Comander.View
{

    public partial class InputWindow : Window
    {
        private string _inputName;

        public InputWindow(string textInfoMessage, string textField = "")
        {
            InitializeComponent();
            this.InfoTextBlock.Text = textInfoMessage;
            InputTextBox.Text = textField;
        }

        public string InputName
        {
            get { return _inputName; }
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            _inputName = string.Empty;
            DialogResult = false;
        }

        private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
        {
            _inputName = this.InputTextBox.Text;
            DialogResult = true;
        }

        private void Move_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
