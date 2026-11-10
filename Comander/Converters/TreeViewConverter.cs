using Comander.ViewModel;
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Comander.Converters
{
    // Tree node -> stroke icon geometry (Themes/Icons.axaml).
    public class TreeViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is File { IsDir: true } ? "Icon.Folder" : "Icon.File";
            return Application.Current != null && Application.Current.TryGetResource(key, null, out var geometry) ? geometry : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
