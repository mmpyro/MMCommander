using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using IOLib;

namespace Comander.Core
{
    public enum ThemeChoice { Dark, Black }
    public enum DateFormatChoice { Iso, Locale, Relative }
    public enum SizeUnitChoice { Auto, Kilobytes, Bytes }

    // Appearance and behaviour settings (Settings window). Stored as <Settings> in the user Config.xml,
    // applied live by rewriting the token brushes of Themes/Dark.axaml.
    public class AppSettings : ObservableObject
    {
        public static readonly string[] Accents = { "#5B9DFF", "#F0A04B", "#3CC6B0", "#B794F6", "#F38BA8" };
        public static readonly string[] MonoCandidates = { "JetBrains Mono", "Cascadia Code", "SF Mono", "Menlo", "Consolas" };

        // Converters and panels read this; tests and the app replace it via Load.
        public static AppSettings Current { get; private set; } = new AppSettings();

        private ThemeChoice _theme = ThemeChoice.Dark;
        private string _accent = Accents[0];
        private bool _comfortable;
        private bool _systemUiFont;
        private string _monoFont = MonoCandidates[0];
        private DateFormatChoice _dateFormat = DateFormatChoice.Iso;
        private SizeUnitChoice _sizeUnits = SizeUnitChoice.Auto;
        private bool _showHidden = true;
        private bool _folderSizes = true;
        private bool _colorIcons = true;
        private bool _separateExt = true;
        private ConflictPolicy _defaultPolicy = ConflictPolicy.Ask;

        public ThemeChoice Theme { get => _theme; set => SetProperty(ref _theme, value); }
        public string Accent { get => _accent; set => SetProperty(ref _accent, Accents.Contains(value) ? value : Accents[0]); }
        public bool Comfortable { get => _comfortable; set => SetProperty(ref _comfortable, value); }
        public bool SystemUiFont { get => _systemUiFont; set => SetProperty(ref _systemUiFont, value); }
        public string MonoFont { get => _monoFont; set => SetProperty(ref _monoFont, value ?? MonoCandidates[0]); }
        public DateFormatChoice DateFormat { get => _dateFormat; set => SetProperty(ref _dateFormat, value); }
        public SizeUnitChoice SizeUnits { get => _sizeUnits; set => SetProperty(ref _sizeUnits, value); }
        // Default for both panels at start; Ctrl+H still toggles one panel.
        public bool ShowHidden { get => _showHidden; set => SetProperty(ref _showHidden, value); }
        // Marking a folder (Space) computes its size in the background.
        public bool FolderSizes { get => _folderSizes; set => SetProperty(ref _folderSizes, value); }
        public bool ColorIcons { get => _colorIcons; set => SetProperty(ref _colorIcons, value); }
        // On: "name" + "ext" columns. Off: full file name, no Ext column.
        public bool SeparateExt { get => _separateExt; set => SetProperty(ref _separateExt, value); }
        public ConflictPolicy DefaultPolicy { get => _defaultPolicy; set => SetProperty(ref _defaultPolicy, value); }

        public double RowHeight => _comfortable ? 34 : 28;

        private static readonly (string key, Func<AppSettings, string> get, Action<AppSettings, string> set)[] Fields =
        {
            ("appearance.theme", s => s.Theme.ToString(), (s, v) => s.Theme = Enum.TryParse(v, out ThemeChoice t) ? t : ThemeChoice.Dark),
            ("appearance.accent", s => s.Accent, (s, v) => s.Accent = v),
            ("appearance.density", s => s.Comfortable ? "comfortable" : "compact", (s, v) => s.Comfortable = v == "comfortable"),
            ("appearance.uiFont", s => s.SystemUiFont ? "system" : "Inter", (s, v) => s.SystemUiFont = v == "system"),
            ("appearance.monoFont", s => s.MonoFont, (s, v) => s.MonoFont = v),
            ("appearance.dateFormat", s => s.DateFormat.ToString(), (s, v) => s.DateFormat = Enum.TryParse(v, out DateFormatChoice d) ? d : DateFormatChoice.Iso),
            ("appearance.sizeUnits", s => s.SizeUnits.ToString(), (s, v) => s.SizeUnits = Enum.TryParse(v, out SizeUnitChoice u) ? u : SizeUnitChoice.Auto),
            ("appearance.showHidden", s => Bool(s.ShowHidden), (s, v) => s.ShowHidden = v != "false"),
            ("appearance.folderSizes", s => Bool(s.FolderSizes), (s, v) => s.FolderSizes = v != "false"),
            ("appearance.colorIcons", s => Bool(s.ColorIcons), (s, v) => s.ColorIcons = v != "false"),
            ("appearance.separateExt", s => Bool(s.SeparateExt), (s, v) => s.SeparateExt = v != "false"),
            ("fileops.defaultPolicy", s => s.DefaultPolicy.ToString(), (s, v) => s.DefaultPolicy = Enum.TryParse(v, out ConflictPolicy p) ? p : ConflictPolicy.Ask),
        };

        private static string Bool(bool b) => b ? "true" : "false";

        public static AppSettings Load(IConfigReader config)
        {
            var settings = new AppSettings();
            foreach (var (key, _, set) in Fields)
            {
                var value = config?.GetSetting(key);
                if (value != null) set(settings, value);
            }
            Current = settings;
            return settings;
        }

        public void Save(IConfigReader config)
        {
            foreach (var (key, get, _) in Fields)
                config.SaveSetting(key, get(this));
        }

        public AppSettings Clone()
        {
            var copy = new AppSettings();
            foreach (var (_, get, set) in Fields) set(copy, get(this));
            return copy;
        }

        public void CopyFrom(AppSettings other)
        {
            foreach (var (_, get, set) in Fields) set(this, get(other));
        }

        // Dark values of the surfaces that "Black" (OLED) replaces.
        private static readonly Dictionary<string, (string dark, string black)> Surfaces = new()
        {
            ["Brush.Bg.Chrome"] = ("#0B0D10", "#000000"),
            ["Brush.Bg.Base"] = ("#0E1014", "#000000"),
            ["Brush.Bg.Toolbar"] = ("#12151A", "#050506"),
            ["Brush.Bg.Pane"] = ("#14171C", "#000000"),
            ["Brush.Bg.PaneHeader"] = ("#101318", "#050506"),
            ["Brush.Bg.Input"] = ("#0F1216", "#08090A"),
            ["Brush.Bg.Dialog"] = ("#171A20", "#0A0B0D"),
        };

        // Pushes the settings into the app resources; every DynamicResource follows immediately.
        public void Apply(Application app)
        {
            if (app == null) return;
            var tokens = FindTokens(app);
            if (tokens != null)
            {
                var accent = Color.Parse(_accent);
                tokens["Accent"] = accent;
                tokens["Brush.Accent"] = new SolidColorBrush(accent);
                tokens["Brush.File.Folder"] = new SolidColorBrush(accent);
                tokens["Brush.Accent.Tint"] = new SolidColorBrush(Color.FromArgb(0x33, accent.R, accent.G, accent.B));
                tokens["Brush.Accent.Marked"] = new SolidColorBrush(Color.FromArgb(0x14, accent.R, accent.G, accent.B));
                tokens["Brush.Accent.Border"] = new SolidColorBrush(Color.FromArgb(0x99, accent.R, accent.G, accent.B));
                foreach (var (key, (dark, black)) in Surfaces)
                    tokens[key] = new SolidColorBrush(Color.Parse(_theme == ThemeChoice.Black ? black : dark));
            }
            app.Resources["RowHeight"] = RowHeight;
            app.Resources["Font.UI"] = _systemUiFont ? FontFamily.Default : new FontFamily("avares://Comander/Fonts#Inter");
            app.Resources["Font.Mono"] = _monoFont == MonoCandidates[0]
                ? new FontFamily("avares://Comander/Fonts#JetBrains Mono")
                : new FontFamily(_monoFont + ", avares://Comander/Fonts#JetBrains Mono");
        }

        // The Dark theme dictionary of Themes/Dark.axaml (merged into App.Resources).
        public static ResourceDictionary FindTokens(Application app)
        {
            foreach (var merged in app.Resources.MergedDictionaries)
            {
                var dict = merged as ResourceDictionary ?? (merged as Avalonia.Markup.Xaml.Styling.ResourceInclude)?.Loaded as ResourceDictionary;
                if (dict == null || !dict.ThemeDictionaries.TryGetValue(ThemeVariant.Dark, out var dark))
                    continue;
                if (dark is ResourceDictionary tokens)
                    return tokens;
            }
            return null;
        }

        public static IEnumerable<string> InstalledMonoFonts()
        {
            var installed = new HashSet<string>(FontManager.Current.SystemFonts.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
            return MonoCandidates.Where((name, i) => i == 0 || installed.Contains(name));
        }
    }
}
