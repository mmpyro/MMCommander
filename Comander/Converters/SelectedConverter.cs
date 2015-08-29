using System;
using System.Globalization;
using System.Windows.Data;

namespace Comander.Converters
{
    public class SelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isSelected = (bool) value;
                if (isSelected)
                {
                    return @"..\Icons\checked.png";
                }
                return @"..\Icons\unchecked.png";
            }
            catch
            {
                return @"..\Icons\unchecked.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}