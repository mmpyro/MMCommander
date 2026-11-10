using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Input;
using Comander.ViewModel;

namespace Comander.Core
{
    public enum CommandScope
    {
        // Runs on the focused panel; its keys only work while a file list has focus.
        Panel,
        // Runs on the window; its keys work anywhere (they all have a modifier or are F-keys).
        App,
    }

    // One user-visible command: what the menus, the key handler, the palette and Settings > Keyboard all list.
    public class AppCommand
    {
        public AppCommand(string id, string label, string group, CommandScope scope, Func<IOManager, ICommand> panel, Func<MainVM, ICommand> app, params string[] keys)
        {
            Id = id;
            Label = label;
            Group = group;
            Scope = scope;
            PanelCommand = panel;
            AppCommandOf = app;
            DefaultKeys = keys;
        }

        public string Id { get; }
        public string Label { get; }
        public string Group { get; }
        public CommandScope Scope { get; }
        public Func<IOManager, ICommand> PanelCommand { get; }
        public Func<MainVM, ICommand> AppCommandOf { get; }
        // Avalonia gesture strings, e.g. "Ctrl+R", "F5", "Back".
        public string[] DefaultKeys { get; }

        public ICommand Resolve(MainVM vm, IOManager panel) =>
            Scope == CommandScope.Panel ? (panel == null ? null : PanelCommand?.Invoke(panel)) : AppCommandOf?.Invoke(vm);
    }

    public static class CommandRegistry
    {
        public const string Navigation = "Navigation", Selection = "Selection", FileOps = "File operations", Panes = "Panes & tabs", Tools = "Tools";
        // Commands the view runs itself (the grid's selection or the path box), not through an ICommand.
        public const string MarkId = "mark", EditPathId = "editPath";

        private static AppCommand P(string id, string label, string group, Func<IOManager, ICommand> cmd, params string[] keys) =>
            new AppCommand(id, label, group, CommandScope.Panel, cmd, null, keys);

        private static AppCommand A(string id, string label, string group, Func<MainVM, ICommand> cmd, params string[] keys) =>
            new AppCommand(id, label, group, CommandScope.App, null, cmd, keys);

        // Order is the match order for keys and the display order in the palette and Settings.
        public static readonly IReadOnlyList<AppCommand> All = new List<AppCommand>
        {
            P("enter", "Open folder", Navigation, p => p.EnterIntoDirCommand, "Enter"),
            P("parent", "Parent folder", Navigation, p => p.ParentDirCommand, "Back", "Escape"),
            P("root", "Root folder", Navigation, p => p.RootDirCommand, "Home"),
            P("back", "Back", Navigation, p => p.BackCommand, "Alt+Left"),
            P("forward", "Forward", Navigation, p => p.ForwardCommand, "Alt+Right"),
            P(EditPathId, "Go to folder… (type a path)", Navigation, null, "Ctrl+L"),
            P("chooseDir", "Choose folder…", Navigation, p => p.ChooseDirCommand),
            P("history", "History…", Navigation, p => p.HistoryCommand),
            P("bookmarks", "Bookmarks…", Navigation, p => p.ShowSortcutsCommand),
            P("addBookmark", "Add bookmark", Navigation, p => p.AddShortCutsCommand, "Ctrl+D"),
            P("tree", "Folder tree…", Navigation, p => p.TreeWindowCommand, "Ctrl+Shift+T"),

            P(MarkId, "Mark / unmark file", Selection, p => p.SelectCommand, "Space", "Insert", "S"),
            P("markAll", "Mark all", Selection, p => p.SelectAllCommand, "Ctrl+A", "A"),
            P("invert", "Invert marks", Selection, p => p.ReverseSelectionCommand, "R"),
            P("markType", "Mark same type", Selection, p => p.SelectTypeCommand, "T"),
            P("copyPaths", "Copy paths to clipboard", Selection, p => p.CopyToClipboardCommand, "Ctrl+C"),
            P("paste", "Paste files", Selection, p => p.PasteToClipboardCommand, "Ctrl+V"),

            P("open", "View / open with default app", FileOps, p => p.RunFileCommand, "F3", "E"),
            P("edit", "Edit", FileOps, p => p.NotepadCommand, "F4", "N"),
            P("newFile", "New file", FileOps, p => p.CreateFileCommand, "Shift+F4"),
            P("copy", "Copy to other pane", FileOps, p => p.CopyCommand, "F5", "C"),
            P("copyHere", "Copy within this pane", FileOps, p => p.CopyHereCommand, "Shift+F5"),
            P("rename", "Rename", FileOps, p => p.RenameCommand, "Shift+F6", "F2"),
            P("move", "Move to other pane", FileOps, p => p.MoveCommand, "F6", "M"),
            P("newFolder", "New folder", FileOps, p => p.CreateDirCommand, "F7", "F1"),
            P("delete", "Delete (to Trash)", FileOps, p => p.DeleteFileCommand, "F8", "Delete"),
            P("deletePermanent", "Delete permanently", FileOps, p => p.DeletePermanentCommand, "Shift+F8", "Shift+Delete"),
            P("terminal", "Terminal here", FileOps, p => p.ConsoleRunCommand, "F9", "Shift+C"),
            P("zip", "Zip…", FileOps, p => p.ZipCommand, "Ctrl+Z"),
            P("unzip", "Unzip…", FileOps, p => p.UnZipCommand, "Ctrl+U"),
            P("info", "Properties", FileOps, p => p.InfoCommand, "Ctrl+I"),
            P("runAsAdmin", "Run as administrator", FileOps, p => p.RunAsAdminCommand, "Ctrl+E"),
            P("refresh", "Refresh", FileOps, p => p.RefreshCommand, "Ctrl+R"),
            P("toggleHidden", "Show / hide hidden files", FileOps, p => p.ToggleHiddenCommand, "Ctrl+H"),
            P("toggleView", "Details / thumbnail view", FileOps, p => p.ToggleViewCommand),

            P("switchPane", "Switch active pane", Panes, p => p.SwitchFocusCommand, "Tab"),
            P("newTab", "New tab", Panes, p => p.NewTabCommand, "Ctrl+T"),
            P("closeTab", "Close tab", Panes, p => p.CloseTabCommand, "Ctrl+W"),
            P("nextTab", "Next tab", Panes, p => p.NextTabCommand, "Ctrl+Tab"),
            A("swapPanes", "Swap panes", Panes, vm => vm.SwitchCommand),
            A("samePath", "Same folder in other pane", Panes, vm => vm.DuplicateCommand),

            A("palette", "Command palette", Tools, vm => vm.Palette.OpenCommand, "Ctrl+K"),
            P("plugins", "Plugins…", Tools, p => p.PluginCommand, "Ctrl+P"),
            A("search", "Search files…", Tools, vm => vm.SearchCommand),
            A("compare", "Compare directories", Tools, vm => vm.CompareDirCommand, "Shift+F2"),
            A("sync", "Synchronize directories", Tools, vm => vm.SyncDirsCommand),
            A("split", "Split file…", Tools, vm => vm.SplitFileCommand),
            A("join", "Join files…", Tools, vm => vm.JoinFileCommand),
            A("reloadDrives", "Reload drives", Tools, vm => vm.RefreshCommand),
            A("openLog", "Open log folder", Tools, vm => vm.ReadLogsCommand),
            A("clearLog", "Clear log", Tools, vm => vm.ClearLogCommand),
            A("keymap", "Keymap (help)", Tools, vm => vm.KeyMapCommand),
            A("syntax", "Filter syntax (help)", Tools, vm => vm.SyntaxCommand),
            A("about", "About", Tools, vm => vm.AboutCommand),
            A("settings", "Settings…", Tools, vm => vm.SettingsCommand, "Ctrl+OemComma"),
        };

        public static AppCommand Get(string id) => All.First(c => c.Id == id);
    }

    // Current gestures per command: defaults, replaced per command by the user's <KeyBindings> in Config.xml.
    // Menus bind to it with {Binding KeyMap[copy]}; it raises "Item[]" when bindings change.
    public class KeyMap : INotifyPropertyChanged
    {
        // Keys that stay Ctrl on macOS: Cmd+H hides the app and Cmd+Tab switches apps.
        private static readonly HashSet<string> CtrlOnMac = new HashSet<string> { "Ctrl+H", "Ctrl+Tab" };
        private readonly Dictionary<string, List<KeyGesture>> _keys = new Dictionary<string, List<KeyGesture>>();
        private readonly bool _mac;

        public KeyMap(IDictionary<string, string> overrides = null, bool? mac = null)
        {
            _mac = mac ?? OperatingSystem.IsMacOS();
            foreach (var command in CommandRegistry.All)
                _keys[command.Id] = Defaults(command).ToList();
            foreach (var pair in overrides ?? new Dictionary<string, string>())
                if (_keys.ContainsKey(pair.Key))
                    _keys[pair.Key] = Parse(pair.Value).ToList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // First gesture of a command (what menus show), or null.
        public KeyGesture this[string id] => _keys.TryGetValue(id, out var list) ? list.FirstOrDefault() : null;

        public IReadOnlyList<KeyGesture> GesturesOf(string id) => _keys.TryGetValue(id, out var list) ? list : new List<KeyGesture>();

        public IEnumerable<KeyGesture> Defaults(AppCommand command)
        {
            foreach (var key in command.DefaultKeys)
            {
                var gesture = KeyGesture.Parse(key);
                // On macOS the ⌘ variant comes first (menus show it), and Ctrl keeps working too.
                if (_mac && gesture.KeyModifiers.HasFlag(KeyModifiers.Control) && !CtrlOnMac.Contains(key))
                    yield return new KeyGesture(gesture.Key, (gesture.KeyModifiers & ~KeyModifiers.Control) | KeyModifiers.Meta);
                yield return gesture;
            }
        }

        public AppCommand Find(KeyEventArgs e, Func<AppCommand, bool> where = null)
        {
            foreach (var command in CommandRegistry.All)
                if ((where == null || where(command)) && _keys[command.Id].Any(g => g.Matches(e)))
                    return command;
            return null;
        }

        public void Set(string id, IEnumerable<KeyGesture> gestures)
        {
            _keys[id] = gestures.ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        // Defaults plus these overrides (a preset, or what was saved before Settings opened).
        public void Load(IDictionary<string, string> overrides)
        {
            foreach (var command in CommandRegistry.All)
                _keys[command.Id] = Defaults(command).ToList();
            foreach (var pair in overrides ?? new Dictionary<string, string>())
                if (_keys.ContainsKey(pair.Key))
                    _keys[pair.Key] = Parse(pair.Value).ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        public void Reset(string id)
        {
            Set(id, Defaults(CommandRegistry.Get(id)));
        }

        public bool IsChanged(string id) =>
            !_keys[id].Select(g => g.ToString()).SequenceEqual(Defaults(CommandRegistry.Get(id)).Select(g => g.ToString()));

        // Only changed commands, as "id" -> "F5;C", for Config.xml.
        public Dictionary<string, string> Overrides() =>
            CommandRegistry.All.Where(c => IsChanged(c.Id)).ToDictionary(c => c.Id, c => string.Join(";", _keys[c.Id].Select(g => g.ToString())));

        private static IEnumerable<KeyGesture> Parse(string value) =>
            (value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(KeyGesture.Parse);
    }
}
