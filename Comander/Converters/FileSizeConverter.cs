using System;
using System.Globalization;
using System.Windows.Data;
using IOLib;

namespace Comander.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double size;
            if (double.TryParse(value.ToString(),out size))
            {
                return Math.Round(size/(double)FileSizeUnit.KB);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}