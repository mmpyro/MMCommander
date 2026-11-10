using Avalonia.Controls;
using Avalonia.Input;

namespace Comander.View
{
    // Borderless popup: drag anywhere to move, Esc closes (result = default).
    public class DialogWindow : Window
    {
        public DialogWindow()
        {
            WindowDecorations = Avalonia.Controls.WindowDecorations.None;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;
            BorderThickness = new Avalonia.Thickness(2);
            if (Avalonia.Application.Current.TryFindResource("MainBorderBrush", out var brush))
                BorderBrush = (Avalonia.Media.IBrush)brush;
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
