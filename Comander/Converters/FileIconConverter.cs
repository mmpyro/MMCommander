using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using IOLib;

namespace Comander.Converters
{
    // Row -> stroke icon geometry (Themes/Icons.axaml): parent entry, folder or file.
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is IAbstractFileStructure f
                ? (f.Name == ".." ? "Icon.ParentEntry" : f.IsDirectory ? "Icon.Folder" : "Icon.File")
                : "Icon.File";
            return Application.Current != null && Application.Current.TryGetResource(key, null, out var geometry) ? geometry : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Row -> extension without the dot; folders (including ".git") have none.
    public class ExtConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IAbstractFileStructure f && !f.IsDirectory ? (f.Ext ?? string.Empty).TrimStart('.') : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
