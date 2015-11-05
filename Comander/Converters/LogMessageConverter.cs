using System;
using System.Globalization;
using System.Windows.Data;
using Comander.Core;

namespace Comander.Converters
{
    public class LogMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogMsg logMsg = value as LogMsg;
            if (logMsg != null)
                return string.Format("[{0}]: {1}", logMsg.Time ,logMsg.Message.Replace(Environment.NewLine,""));
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}