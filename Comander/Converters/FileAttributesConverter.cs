using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Data;

namespace Comander.Converters
{
    public class FileAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var attributes = (FileAttributes)value;
                return PerformAttributes( attributes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string PerformAttributes(FileAttributes attributes)
        {
            var sb = new StringBuilder();
            if (attributes == FileAttributes.Hidden)
                sb.Append("H");
            if (attributes == FileAttributes.ReadOnly)
                sb.Append("R");
            if (attributes == FileAttributes.Temporary)
                sb.Append("T");
            if (attributes == FileAttributes.Encrypted)
                sb.Append("E");
            if (attributes == FileAttributes.Compressed)
                sb.Append("C");
            if (attributes == FileAttributes.Archive)
                sb.Append("A");
            if (attributes == FileAttributes.Directory)
                sb.Append("D");
            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}