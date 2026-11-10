using System;
using System.Globalization;
using System.Text;
using Avalonia.Input;

namespace Comander.Converters
{
    // KeyGesture -> key-cap text: "⌘K" / "⇧F8" on macOS, "Ctrl+K" elsewhere.
    public class GestureTextConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is KeyGesture gesture ? Format(gesture) : string.Empty;

        public static string Format(KeyGesture gesture, bool? mac = null)
        {
            if (gesture == null) return string.Empty;
            if (!(mac ?? OperatingSystem.IsMacOS())) return gesture.ToString().Replace("OemComma", ",");
            var text = new StringBuilder();
            var m = gesture.KeyModifiers;
            if (m.HasFlag(KeyModifiers.Control)) text.Append('⌃');
            if (m.HasFlag(KeyModifiers.Alt)) text.Append('⌥');
            if (m.HasFlag(KeyModifiers.Shift)) text.Append('⇧');
            if (m.HasFlag(KeyModifiers.Meta)) text.Append('⌘');
            text.Append(gesture.Key switch
            {
                Key.Back => "⌫",
                Key.Enter => "↵",
                Key.Escape => "Esc",
                Key.Left => "←",
                Key.Right => "→",
                Key.Tab => "⇥",
                Key.OemComma => ",",
                Key.Up => "↑",
                Key.Down => "↓",
                _ => new KeyGesture(gesture.Key).ToString(),
            });
            return text.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
