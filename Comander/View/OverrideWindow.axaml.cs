using Avalonia.Interactivity;
using Comander.Dtos;

namespace Comander.View
{
    // ShowDialog<CopyOption>: closing the window counts as "don't override" (CopyOption.None).
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
    }
}
