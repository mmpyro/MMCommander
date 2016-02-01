using Comander.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Comander.Converters
{
    public class TreeViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            File file = value as File;
            if(file != null)
            {
                if (file.IsDir)
                {
                    return @"..\Icons\treefolder.png";
                }
                return @"..\Icons\treefiles.png";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
