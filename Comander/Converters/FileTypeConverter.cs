using System;
using System.Globalization;
using System.Windows.Data;

namespace Comander.Converters
{
    public class FileTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isDir = bool.Parse(value.ToString());
            if (isDir)
            {
                return "Dir";
            }
            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}