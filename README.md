# MMCommander

A keyboard-driven two-panel file manager for Windows, macOS and Linux.
Built on .NET 10, Avalonia 12 and CommunityToolkit.Mvvm.

It does copy, move, delete and rename with overwrite prompts. It also has
- zip, tar.gz and tar.bz2 archiving (zip can be AES-encrypted)
- file split and join
- recursive search
- a small filter language
- folder shortcuts and per-panel history
- user-defined external commands
- Plugins (.NET class libraries)

## Quick start

Requirements: .NET SDK 10 (`global.json` pins 10.0.403 and rolls forward to newer feature bands).

```sh
dotnet run --project Comander          # start the app
dotnet test MMComander.sln             # run all tests (about 100, under 10 s)
```

Publish a self-contained single-file build:

```sh
dotnet publish Comander -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
dotnet publish Comander -c Release -r win-x64   --self-contained -p:PublishSingleFile=true
dotnet publish Comander -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

Keep `Config.xml`, `Commands.xml`, `Manuals/` and `Plugins/` next to the binary. The app reads them from `AppContext.BaseDirectory`, so it works from any working directory.

## Keyboard

The keys work in the focused file panel.

| Key | Action | Key | Action |
|---|---|---|---|
| Enter | Open folder | Tab | Switch panel |
| Backspace / Esc | Parent folder | Home | Drive root |
| F3 / E | Open with default app | F4 / N | Open in editor |
| F5 / C | Copy to other panel | F6 / M | Move to other panel |
| F7 / F1 | New folder | Shift+F4 | New file |
| F8 / Delete | Move to Trash (asks first) | Shift+F6 / F2 | Rename |
| Shift+F8 | Delete permanently | Shift+F5 | Copy within this panel |
| F9 / Shift+C | Terminal here | Ctrl+R | Refresh |
| Space / Insert / S | Mark / unmark file | Ctrl+A / A | Mark all |
| R | Invert marks | T | Mark all of this type |
| Ctrl+Z | Zip selection | Ctrl+U | Extract archive |
| Ctrl+C | Copy files to clipboard | Ctrl+V | Paste files from clipboard |
| Ctrl+I | File info | Ctrl+D | Add folder bookmark |
| Ctrl+P | Plugins | Ctrl+Shift+T | Folder tree |
| Ctrl+E | Run as admin (Windows) | Ctrl+H | Hide / show dot files and folders |
| Ctrl+T | New tab | Ctrl+W | Close tab |
| Alt+Left | Back | Alt+Right | Forward |
| Ctrl+Tab | Next tab | Ctrl+L | Type a path |
| Ctrl+K | Command palette | Shift+F2 | Compare directories |

The toolbar, the menus and the F-key bar at the bottom act on the panel that last had focus; that panel has the blue border. Each panel has folder tabs with their own back/forward history, and the open tabs are saved on exit. Click a part of the path to jump there, or click the empty part of the path bar to type a path.

F5 and F6 open an options dialog: destination, what to do when a file exists (ask, overwrite older, skip, keep both), timestamps, SHA-256 verify, symlinks. The copy then runs as a job with a progress window (pause, cancel, run in background); **Add to queue** runs it after the current one. With nothing marked, the file under the cursor is used. F8 moves to the Trash (Windows Recycle Bin, macOS `~/.Trash`, Linux freedesktop Trash); files on another volume can only be deleted permanently.

The toolbar's view switch (list / grid icons) shows the active panel as thumbnails: images get a preview, everything else a large icon. Keys, marking and the cursor work the same in both views.

**Settings** (View → Settings…, Ctrl+, / ⌘,): theme (Dark or Black for OLED), accent colour, row density, fonts, date and size formats, hidden files, folder sizes on Space, coloured icons, a separate Ext column, the default conflict policy, and every keyboard shortcut (record, presets, conflicts, export). Changes show immediately; **Done** saves them to the user `Config.xml`, Cancel undoes them.

**Ctrl+K** (⌘K on macOS) opens the command palette: type part of a command name, a file name in the active panel, or a full folder path, then press Enter. On macOS every Ctrl shortcut also works with ⌘, except Ctrl+H and Ctrl+Tab.

All shortcuts are defined in one place, `Comander/Core/CommandRegistry.cs`. To change one, add it to the user `Config.xml`:

```xml
<KeyBindings>
  <Binding command="copy" keys="F5;Ctrl+Shift+C" />
</KeyBindings>
```

The status bar shows the latest message: errors in red, warnings in orange. Hover it for the full text, or click **View log** for every message of this session. While there are errors you haven't looked at, the button turns into a red "N errors · View log" chip. Full stack traces go to the log file (Help → Open log folder).

## Filter language

Type in the filter box under a panel. The list updates 1 s after the last key press.

| Expression | Meaning |
|---|---|
| `ext == pdf` / `ext != pdf` | Extension is / is not `pdf` |
| `ext -> (pdf, txt)` / `ext <> (pdf, txt)` | Extension is in / not in the set |
| `name == report` | Name without extension equals |
| `name like rep` | Name contains |
| `a & b`, `a \| b` | And / or |

Example: `ext -> (xml, pdf) & name like il`.

## Folder shortcuts

Type a shortcut into the path box: `%temp%`, `%desktop%`, `%documents%`, `%programdata%`, `%pf%`, `%pf86%`, `%windows%`. Add your own with Ctrl+D.

## Archives

| Format | Create | Extract |
|---|---|---|
| zip (optionally AES-256) | yes | yes |
| tar, tar.gz, tar.bz2 | yes, without a password | yes |
| 7z, rar, gz | no | yes |

Extraction refuses entries that would land outside the target folder (zip-slip).

## Configuration

| File | Location | Purpose |
|---|---|---|
| `Config.xml` (defaults) | next to the binary | Editor and terminal per OS |
| `Config.xml` (user) | macOS `~/Library/Application Support/MMCommander/`, Windows `%APPDATA%\MMCommander\`, Linux `~/.config/MMCommander/` | Last panel paths and shortcuts. Written on exit, atomically |
| `Commands.xml` | next to the binary | External commands for the Commands menu |
| logs | NLog, see `LogLib/NLog.config` | 10 MB per file, 7 archives |

`Config.xml` programs: each space-separated token in `args` is one argument, and `{path}` becomes the selected file, so paths with spaces stay intact:

```xml
<Program name="notepad" os="osx" args="-t {path}">open</Program>
```

`Commands.xml` commands: `{s}` is the file selected in the left panel and `{d}` the file selected in the right one:

```xml
<Command name="Compare Files"><Parameter>kdiff3 -args {s} {d}</Parameter></Command>
```

## Plugins

Put a .NET 10 class library in `Plugins/<Name>/` next to the binary. Plugins run with your full user rights, so install only ones you trust. How to write one: [docs/PLUGINS.md](docs/PLUGINS.md). A working sample, `SamplePlugins/Sha256Sums`, writes a `SHA256SUMS` file for the selected files.

## Known limits

- 7z archives can be extracted but not created.
- The app has been checked by automated tests and headless rendering on macOS only. Windows and Linux builds publish, but nobody has clicked through them.
- File clipboard (Ctrl+C / Ctrl+V) depends on the OS clipboard's file support. It is untested on Linux.
- Split runs in the background but has no progress bar.
