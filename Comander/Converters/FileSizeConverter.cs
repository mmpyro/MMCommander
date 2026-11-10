using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Comander.Core;
using IOLib;

namespace Comander.Converters
{
    // The Size column: the row (folders "—", or their computed size once marked; ".." nothing) or a raw byte count.
    // Units follow Settings > Size units. Format/FormatSpace are the fixed "auto" units used everywhere else.
    public class FileSizeConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Row(value, null);

        // [0] the row, [1] MetaData.FolderSize (null until computed).
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture) =>
            Row(values.Count > 0 ? values[0] : null, values.Count > 1 ? values[1] as long? : null);

        private static string Row(object value, long? folderSize)
        {
            if (value is IAbstractFileStructure file)
            {
                if (file.Name == "..") return string.Empty;
                if (file.IsDirectory) return folderSize.HasValue ? List(folderSize.Value) : "—";
                return List(file.Size);
            }
            return value is long or int ? List(System.Convert.ToInt64(value)) : string.Empty;
        }

        public static string List(long bytes) => AppSettings.Current.SizeUnits switch
        {
            SizeUnitChoice.Kilobytes => Math.Round(bytes / 1024d).ToString("#,0", CultureInfo.InvariantCulture) + " KB",
            SizeUnitChoice.Bytes => bytes.ToString("#,0", CultureInfo.InvariantCulture) + " B",
            _ => Format(bytes),
        };

        // KB below 1 MB, one decimal above.
        public static string Format(long bytes)
        {
            const double mb = 1024d * 1024, gb = mb * 1024;
            if (bytes < mb) return Math.Round(bytes / 1024d).ToString(CultureInfo.InvariantCulture) + " KB";
            if (bytes < gb) return (bytes / mb).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            return (bytes / gb).ToString("0.0", CultureInfo.InvariantCulture) + " GB";
        }

        // Whole GB/TB for disk sizes ("412 GB free of 994 GB").
        public static string FormatSpace(long bytes)
        {
            const double gb = 1024d * 1024 * 1024, tb = gb * 1024;
            if (bytes < gb) return Format(bytes);
            if (bytes < tb) return Math.Round(bytes / gb).ToString(CultureInfo.InvariantCulture) + " GB";
            return (bytes / tb).ToString("0.0", CultureInfo.InvariantCulture) + " TB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Settings > Separate extension column: "report" + "pdf", or the full "report.pdf".
    public class DisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IAbstractFileStructure f) return string.Empty;
            if (!AppSettings.Current.SeparateExt || f.IsDirectory || string.IsNullOrEmpty(f.Ext) || f.Ext == f.Name) return f.Name;
            return System.IO.Path.GetFileNameWithoutExtension(f.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
