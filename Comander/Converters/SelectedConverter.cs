using System;
using System.Globalization;
using Avalonia.Data.Converters;

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
                    return Icons.Load("checked.png");
                }
                return Icons.Load("unchecked.png");
            }
            catch
            {
                return Icons.Load("unchecked.png");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}