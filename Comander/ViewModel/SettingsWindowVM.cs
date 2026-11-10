using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Comander.Converters;
using Comander.Core;
using Comander.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOLib;

namespace Comander.ViewModel
{
    public class SwatchVM
    {
        public string Name { get; init; }
        public string Hex { get; init; }
        public IBrush Brush => new SolidColorBrush(Color.Parse(Hex));
        public bool IsSelected { get; init; }
    }

    public class PreviewRowVM
    {
        public string Name { get; init; }
        public string Size { get; init; }
        public Geometry Icon { get; init; }
        public IBrush IconBrush { get; init; }
        public bool IsDir { get; init; }
        public bool IsCursor { get; init; }
    }

    public class KeyRowVM : ObservableObject
    {
        private bool _isRecording;

        public KeyRowVM(AppCommand command, KeyMap keys)
        {
            Command = command;
            Keys = keys.GesturesOf(command.Id).Select(g => GestureTextConverter.Format(g)).ToList();
            IsChanged = keys.IsChanged(command.Id);
        }

        public AppCommand Command { get; }
        public string Label => Command.Label;
        public List<string> Keys { get; }
        public bool HasNoKeys => Keys.Count == 0;
        public bool IsChanged { get; }

        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }
    }

    public class KeyGroupVM
    {
        public string Name { get; init; }
        public List<KeyRowVM> Rows { get; init; }
    }

    // Settings window (docs/design/Settings.dc.html, Shortcuts.dc.html). Appearance changes apply live;
    // Done saves them and the key bindings to Config.xml, Cancel/Esc restores what was there.
    public class SettingsWindowVM : ObservableObject
    {
        public static readonly string[] Presets = { "Total Commander (modified)", "Total Commander", "Finder-like" };

        private readonly IConfigReader _config;
        private readonly AppSettings _before;
        private readonly Dictionary<string, string> _keysBefore;
        private int _section;
        private string _search = string.Empty;
        private bool _changedOnly;
        private int _preset;
        private KeyRowVM _recording;
        private string _conflictText;
        private KeyGesture _pending;
        private AppCommand _conflictOwner;
        private List<KeyGroupVM> _groups = new List<KeyGroupVM>();

        public SettingsWindowVM(KeyMap keys, IConfigReader config)
        {
            KeyMap = keys;
            _config = config;
            _before = Settings.Clone();
            _keysBefore = keys.Overrides();
            Settings.PropertyChanged += OnSettingsChanged;
            SetAccentCommand = new RelayCommand<string>(hex => Settings.Accent = hex);
            RecordCommand = new RelayCommand<KeyRowVM>(StartRecording);
            ReassignCommand = new RelayCommand(Reassign);
            CancelConflictCommand = new RelayCommand(StopRecording);
            ResetPresetCommand = new RelayCommand(() => ApplyPreset(_preset));
            ToggleChangedCommand = new RelayCommand(() => ChangedOnly = !ChangedOnly);
            ResetDefaultsCommand = new RelayCommand(() => Settings.CopyFrom(new AppSettings()));
            BuildGroups();
        }

        public AppSettings Settings => AppSettings.Current;
        public KeyMap KeyMap { get; }
        public RelayCommand<string> SetAccentCommand { get; }
        public RelayCommand<KeyRowVM> RecordCommand { get; }
        public RelayCommand ReassignCommand { get; }
        public RelayCommand CancelConflictCommand { get; }
        public RelayCommand ResetPresetCommand { get; }
        public RelayCommand ToggleChangedCommand { get; }
        public RelayCommand ResetDefaultsCommand { get; }

        // 0 Appearance, 1 Keyboard, 2 File operations.
        public int Section
        {
            get => _section;
            set
            {
                if (!SetProperty(ref _section, Math.Max(0, value))) return;
                OnPropertyChanged(nameof(IsAppearance));
                OnPropertyChanged(nameof(IsKeyboard));
                OnPropertyChanged(nameof(IsFileOps));
            }
        }

        public bool IsAppearance => _section == 0;
        public bool IsKeyboard => _section == 1;
        public bool IsFileOps => _section == 2;

        public int ThemeIndex { get => (int)Settings.Theme; set => Settings.Theme = (ThemeChoice)Math.Max(0, value); }
        public int DensityIndex { get => Settings.Comfortable ? 1 : 0; set => Settings.Comfortable = value == 1; }
        public int UiFontIndex { get => Settings.SystemUiFont ? 1 : 0; set => Settings.SystemUiFont = value == 1; }
        public int DateIndex { get => (int)Settings.DateFormat; set => Settings.DateFormat = (DateFormatChoice)Math.Max(0, value); }
        public int UnitsIndex { get => (int)Settings.SizeUnits; set => Settings.SizeUnits = (SizeUnitChoice)Math.Max(0, value); }
        public int PolicyIndex { get => (int)Settings.DefaultPolicy; set => Settings.DefaultPolicy = (ConflictPolicy)Math.Max(0, value); }
        public IEnumerable<string> MonoFonts { get; } = AppSettings.InstalledMonoFonts().ToList();

        public bool AskBeforeTrash
        {
            get => _config?.GetSetting(DialogService.SkipTrashConfirmKey) != "true";
            set { _config?.SaveSetting(DialogService.SkipTrashConfirmKey, value ? null : "true"); OnPropertyChanged(); }
        }

        public List<SwatchVM> Swatches => AppSettings.Accents.Zip(new[] { "Blue", "Amber", "Teal", "Violet", "Rose" },
            (hex, name) => new SwatchVM { Hex = hex, Name = name, IsSelected = hex == Settings.Accent }).ToList();

        public List<PreviewRowVM> Preview
        {
            get
            {
                var rows = new List<(string name, string ext, long size, bool dir)>
                {
                    ("repos", "", 0, true), ("scripts", "", 0, true), ("tmp", "", 0, true),
                    (".zshrc", "", 4096, false), ("canopy_v1.2.0_darwin_arm64.tar", ".gz", 3_145_728, false),
                    ("cert-manager", ".yaml", 0, false), ("image", ".png", 1_572_864, false),
                };
                if (!Settings.ShowHidden) rows.RemoveAll(r => r.name.StartsWith('.'));
                return rows.Select((r, i) => new PreviewRowVM
                {
                    Name = r.dir || Settings.SeparateExt ? r.name : r.name + r.ext,
                    Size = r.dir ? "—" : FileSizeConverter.List(r.size),
                    IsDir = r.dir,
                    IsCursor = i == 3,
                    Icon = Resource<Geometry>(r.dir ? "Icon.Folder" : "Icon.File"),
                    IconBrush = Resource<IBrush>(!Settings.ColorIcons && !r.dir ? "Brush.File.Other" : FileKindBrushConverter.KeyFor(r.dir, r.ext)),
                }).ToList();
            }
        }

        public double PreviewRowHeight => Settings.RowHeight;

        private static T Resource<T>(string key) where T : class =>
            Application.Current != null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var value) ? value as T : null;

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Swatches));
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(PreviewRowHeight));
            OnPropertyChanged(nameof(ThemeIndex));
            OnPropertyChanged(nameof(DensityIndex));
            OnPropertyChanged(nameof(UiFontIndex));
            OnPropertyChanged(nameof(DateIndex));
            OnPropertyChanged(nameof(UnitsIndex));
            OnPropertyChanged(nameof(PolicyIndex));
        }

        // ---- Keyboard ----

        public int Preset
        {
            get => _preset;
            set { if (SetProperty(ref _preset, Math.Max(0, value))) ApplyPreset(_preset); }
        }

        public string Search
        {
            get => _search;
            set { if (SetProperty(ref _search, value ?? string.Empty)) BuildGroups(); }
        }

        public bool ChangedOnly
        {
            get => _changedOnly;
            set { if (SetProperty(ref _changedOnly, value)) BuildGroups(); }
        }

        public string ChangedText => "Changed only · " + CommandRegistry.All.Count(c => KeyMap.IsChanged(c.Id));

        public List<KeyGroupVM> Groups
        {
            get => _groups;
            private set => SetProperty(ref _groups, value);
        }

        public KeyRowVM Recording => _recording;

        public string ConflictText
        {
            get => _conflictText;
            private set
            {
                if (SetProperty(ref _conflictText, value))
                    OnPropertyChanged(nameof(HasConflict));
            }
        }

        public bool HasConflict => _conflictText != null;

        private void BuildGroups()
        {
            var q = _search.Trim();
            Groups = CommandRegistry.All
                .Select(c => new KeyRowVM(c, KeyMap))
                .Where(r => !_changedOnly || r.IsChanged)
                .Where(r => q.Length == 0 || r.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || r.Keys.Any(k => k.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(r => r.Command.Group)
                .Select(g => new KeyGroupVM { Name = g.Key, Rows = g.ToList() })
                .ToList();
            OnPropertyChanged(nameof(ChangedText));
        }

        private void StartRecording(KeyRowVM row)
        {
            StopRecording();
            _recording = row;
            if (row != null) row.IsRecording = true;
            OnPropertyChanged(nameof(Recording));
        }

        private void StopRecording()
        {
            if (_recording != null) _recording.IsRecording = false;
            _recording = null;
            _pending = null;
            _conflictOwner = null;
            ConflictText = null;
            OnPropertyChanged(nameof(Recording));
        }

        // Called by the window for each key press while a row records. Returns true when the key was used.
        public bool RecordKey(Key key, KeyModifiers modifiers)
        {
            if (_recording == null) return false;
            if (key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return true;
            if (key == Key.Escape && modifiers == KeyModifiers.None)
            {
                StopRecording();
                return true;
            }
            var id = _recording.Command.Id;
            if (key == Key.Back && modifiers == KeyModifiers.None)
            {
                KeyMap.Set(id, Array.Empty<KeyGesture>());
                StopRecording();
                BuildGroups();
                return true;
            }
            var gesture = new KeyGesture(key, modifiers);
            var owner = CommandRegistry.All.FirstOrDefault(c => c.Id != id && KeyMap.GesturesOf(c.Id).Any(g => g.ToString() == gesture.ToString()));
            if (owner != null)
            {
                _pending = gesture;
                _conflictOwner = owner;
                ConflictText = string.Format("{0} is already used by “{1}”.", GestureTextConverter.Format(gesture), owner.Label);
                return true;
            }
            KeyMap.Set(id, new[] { gesture });
            StopRecording();
            BuildGroups();
            return true;
        }

        private void Reassign()
        {
            if (_recording == null || _pending == null || _conflictOwner == null) return;
            var gesture = _pending.ToString();
            KeyMap.Set(_conflictOwner.Id, KeyMap.GesturesOf(_conflictOwner.Id).Where(g => g.ToString() != gesture).ToList());
            KeyMap.Set(_recording.Command.Id, new[] { _pending });
            StopRecording();
            BuildGroups();
        }

        public void ApplyPreset(int preset)
        {
            KeyMap.Load(PresetOverrides(preset, KeyMap));
            StopRecording();
            BuildGroups();
        }

        // Preset = overrides on top of the defaults. "Total Commander" drops the single-letter aliases (frees typing);
        // "Finder-like" adds ⌘ arrows and ⌘⌫ on top of that.
        public static Dictionary<string, string> PresetOverrides(int preset, KeyMap keys)
        {
            var result = new Dictionary<string, string>();
            if (preset == 0) return result;
            foreach (var command in CommandRegistry.All)
            {
                var defaults = keys.Defaults(command).ToList();
                var kept = defaults.Where(g => !(g.KeyModifiers == KeyModifiers.None && g.Key >= Key.A && g.Key <= Key.Z)).ToList();
                if (kept.Count != defaults.Count)
                    result[command.Id] = string.Join(";", kept.Select(g => g.ToString()));
            }
            if (preset == 2)
            {
                result["parent"] = "Cmd+Up;Back";
                result["enter"] = "Enter;Cmd+Down";
                result["delete"] = "Cmd+Back;F8";
                result["newFolder"] = "Cmd+Shift+N;F7";
            }
            return result;
        }

        public void Save()
        {
            StopRecording();
            Settings.Save(_config);
            _config.SaveKeyBindings(KeyMap.Overrides());
            Settings.PropertyChanged -= OnSettingsChanged;
        }

        public void Revert()
        {
            StopRecording();
            Settings.CopyFrom(_before);
            KeyMap.Load(_keysBefore);
            Settings.PropertyChanged -= OnSettingsChanged;
        }

        // Export…: the user's changes as a <KeyBindings> block for Config.xml.
        public string ExportText()
        {
            var lines = KeyMap.Overrides().Select(p => string.Format("  <Binding command=\"{0}\" keys=\"{1}\" />", p.Key, p.Value));
            return "<KeyBindings>\n" + string.Join("\n", lines) + "\n</KeyBindings>\n";
        }
    }
}
