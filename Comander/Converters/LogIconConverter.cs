using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Comander.Core;
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
                        return Icons.Load("error.png");
                    case LogInfo.Warrning:
                        return Icons.Load("warning.png");
                    default:
                        return Icons.Load("info.png");
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