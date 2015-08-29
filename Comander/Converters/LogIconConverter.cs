using System;
using System.Globalization;
using System.Windows.Data;
using Comander.Other;
using LogLib;

namespace Comander.Converters
{
    public class LogIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogMsg logMsg = value as LogMsg;
            if (logMsg != null)
            {
                switch (logMsg.Info)
                {
                    case LogInfo.Error:
                        return @"\Icons\error.png";
                    case LogInfo.Warrning:
                        return @"\Icons\warning.png";
                    default:
                        return @"\Icons\info.png";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}