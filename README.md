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
- MEF plugins

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
| Escape | Parent folder | Home | Drive root |
| C | Copy to other panel | M | Move to other panel |
| Delete | Delete (asks first) | Ctrl+R | Rename |
| F1 | New folder | F2 | New file |
| F5 | Refresh | Ctrl+T | Folder tree |
| S | Select / unselect file | A | Select all |
| R | Reverse selection | T | Select all of this type |
| E | Open with default app | Ctrl+E | Run as admin (Windows) |
| N | Open in editor | Shift+C | Terminal here |
| Ctrl+Z | Zip selection | Ctrl+U | Extract archive |
| Ctrl+C | Copy files to clipboard | Ctrl+V | Paste files from clipboard |
| Ctrl+I | File info | Ctrl+D | Add folder shortcut |
| F3 | Plugins | | |

The menu also has Switch, Source = Target, Compare Dir, Split, Join, Search, logs and the manuals.

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

Drop a .NET 10 class library into `Plugins/` next to the binary. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#plugins).

## Known limits

- 7z archives can be extracted but not created.
- The app has been checked by automated tests and headless rendering on macOS only. Windows and Linux builds publish, but nobody has clicked through them.
- File clipboard (Ctrl+C / Ctrl+V) depends on the OS clipboard's file support. It is untested on Linux.
- Split runs in the background but has no progress bar.
