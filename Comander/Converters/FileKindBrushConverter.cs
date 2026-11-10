using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using IOLib;

namespace Comander.Converters
{
    // Row -> icon brush: folders use the accent, files are coloured by extension (Themes/Dark.axaml Brush.File.*).
    public class FileKindBrushConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> Kinds = Build();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is IAbstractFileStructure file ? KeyFor(file.IsDirectory, file.Ext)
                : value is ViewModel.File node ? KeyFor(node.IsDir, System.IO.Path.GetExtension(node.Path))
                : "Brush.File.Other";
            // Settings > Colour file icons by type (folders keep the accent).
            if (!Core.AppSettings.Current.ColorIcons && key != "Brush.File.Folder") key = "Brush.File.Other";
            return Application.Current != null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var brush)
                ? brush
                : null;
        }

        public static string KeyFor(bool isDirectory, string ext)
        {
            if (isDirectory) return "Brush.File.Folder";
            var normalized = (ext ?? string.Empty).TrimStart('.').ToLowerInvariant();
            return Kinds.TryGetValue(normalized, out var key) ? key : "Brush.File.Other";
        }

        private static Dictionary<string, string> Build()
        {
            var map = new Dictionary<string, string>();
            void Add(string key, params string[] exts) { foreach (var e in exts) map[e] = key; }
            Add("Brush.File.Image", "png", "jpg", "jpeg", "gif", "bmp", "webp", "svg", "ico", "tif", "tiff", "heic");
            Add("Brush.File.Archive", "zip", "gz", "tgz", "tar", "rar", "7z", "bz2", "xz", "zst");
            Add("Brush.File.Config", "yaml", "yml", "json", "toml", "ini", "xml", "conf", "config", "env");
            Add("Brush.File.Data", "sql", "csv", "tsv", "db", "sqlite", "parquet");
            return map;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
