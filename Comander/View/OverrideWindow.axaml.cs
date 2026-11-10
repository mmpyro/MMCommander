using Avalonia.Input;
using Avalonia.Interactivity;
using Comander.Dtos;

namespace Comander.View
{
    // ShowDialog<CopyOption>: Escape means Cancel, like the Cancel button.
    public partial class OverrideWindow : DialogWindow
    {
        public OverrideWindow(string textMessage, WindowIcon icon = WindowIcon.Warrning)
        {
            InitializeComponent();
            InfoTextBlock.Text = textMessage;
            Image.Source = ConfirmWindow.IconFor(icon);
        }

        private void OverrideSingleButtonClick(object sender, RoutedEventArgs e) => Close(CopyOption.OverrideSingle);

        private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Close(CopyOption.Cancel);

        private void OverrideAllButtonClick(object sender, RoutedEventArgs e) => Close(CopyOption.OverrideAll);

        private void DotNotOverrideButtonClick(object sender, RoutedEventArgs e) => Close(CopyOption.None);

        // DialogWindow closes on Escape with default(CopyOption) = None, which would keep the copy asking.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close(CopyOption.Cancel);
                return;
            }
            base.OnKeyDown(e);
        }
    }
}
