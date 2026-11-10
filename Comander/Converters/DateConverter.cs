using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Comander.Core;

namespace Comander.Converters
{
    // Settings > Date format: ISO "yyyy-MM-dd HH:mm" (default), the OS locale, or relative ("3 weeks ago").
    // The ".." entry has no real date (MinValue) and shows nothing.
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is DateTime date ? Format(date, AppSettings.Current.DateFormat, DateTime.Now) : string.Empty;

        public static string Format(DateTime date, DateFormatChoice format, DateTime now)
        {
            if (date == DateTime.MinValue) return string.Empty;
            switch (format)
            {
                case DateFormatChoice.Locale:
                    return date.ToString("g", CultureInfo.CurrentCulture);
                case DateFormatChoice.Relative:
                    return Relative(now - date);
                default:
                    return date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }
        }

        private static string Relative(TimeSpan age)
        {
            if (age < TimeSpan.Zero) return "in the future";
            if (age.TotalMinutes < 1) return "just now";
            if (age.TotalHours < 1) return Unit((int)age.TotalMinutes, "minute");
            if (age.TotalDays < 1) return Unit((int)age.TotalHours, "hour");
            if (age.TotalDays < 7) return Unit((int)age.TotalDays, "day");
            if (age.TotalDays < 31) return Unit((int)(age.TotalDays / 7), "week");
            if (age.TotalDays < 365) return Unit((int)(age.TotalDays / 30), "month");
            return Unit((int)(age.TotalDays / 365), "year");
        }

        private static string Unit(int n, string word) => n + " " + word + (n == 1 ? string.Empty : "s") + " ago";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
