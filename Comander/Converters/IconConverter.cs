using System;
using System.Globalization;
using System.Windows.Data;
using LogLib;

namespace Comander.Converters
{
    public class IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogInfo info = (LogInfo)value;
            if (info.Equals(LogInfo.Error))
                return @"..\Icons\Cancel-48.png";
            else if(info.Equals(LogInfo.Warrning))
                return @"..\Icons\Alert-48.png";
            return @"..\Icons\info_black.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}