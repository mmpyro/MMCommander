using Avalonia.Controls;
using Avalonia.Input;

namespace Comander.View
{
    // Borderless popup card: drag anywhere to move, Esc closes (result = default).
    public class DialogWindow : Window
    {
        public DialogWindow()
        {
            WindowDecorations = Avalonia.Controls.WindowDecorations.None;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;
            // Rounded card look (Themes/Theme.axaml DialogWindow template): transparent window, the card draws the corners.
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
            Background = Avalonia.Media.Brushes.Transparent;
            Classes.Add("dialog");
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not TextBox)
                BeginMoveDrag(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
