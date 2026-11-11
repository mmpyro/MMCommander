# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

MMCommander is a keyboard-driven two-panel file manager: .NET 10, Avalonia 12, CommunityToolkit.Mvvm. User docs are in `README.md` and design notes in `docs/ARCHITECTURE.md`. Read those before larger changes.

## Commands

```sh
dotnet build MMComander.sln                     # build everything (note the sln spelling: "Comander")
dotnet run --project Comander                   # start the app
dotnet test MMComander.sln                      # all tests, about 10 s
dotnet test IOLibTest --filter Name=JoinFile_Test             # one test
dotnet test ComanderTest --filter FullyQualifiedName~ProxyIOTest  # one class
dotnet test IOLibTest --filter Name=Split_Over2GB_WritesAllParts  # [Explicit] 2.2 GB test, never runs by default
MMC_SCREENSHOTS=/some/dir dotnet test ComanderTest   # save PNGs of each headless-rendered window
sh docs/make-screenshots.sh                     # regenerate the README images (docs/images) from a demo folder; needs ffmpeg
dotnet publish Comander -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
git tag vX.Y.Z && git push origin vX.Y.Z           # .github/workflows/release.yml builds 3 platforms and publishes the Release
```

CI (`.github/workflows/ci.yml`) runs on PRs to `master` on Linux, Windows and macOS: `dotnet build -c Release -p:TreatWarningsAsErrors=true -p:WarningsNotAsErrors=CS0067`, then `dotnet test`. Keep both green locally before pushing.

There is no linter. The only accepted build warning is CS0067 in `LogLib/FileLogger.cs`.

`global.json` pins SDK 10.0.403 (rolls forward). `Directory.Build.props` sets net10.0 for every project, with **implicit usings and nullable both off**, so write explicit `using`s and no `?` reference annotations.

## Architecture

Project graph: `Comander` (the Avalonia app) references `IOLib` (file model and operations), `IOLinq` (filter-language parser), `ZipAdapter`/`ZipLib.csproj` (SharpZipLib writes, SharpCompress extracts), `LogLib` (NLog + GUI logger) and `CommanderPlugin` (plugin contract). The app folder is spelled `Comander`, with one "m".

- **Composition root:** `App.CreateMainVm` in `Comander/App.axaml.cs` wires everything by hand. There is deliberately no DI container. The two panels are two `IOManager` instances (`ManagerType.Io1`/`Io2`) that point at each other through `SecondManager`. Tests call `CreateMainVm` to get the real graph.
- **Transfers:** F5/F6/F8 go `ProxyIO` → `IDialogService.AskCopy/AskDelete` → a `TransferJob` on the shared `Core/JobQueue` (one job at a time, holds `StateMonitor`). The copy itself is `IOLib/Transfer.cs` (`TransferEngine`: chunked copy, progress, pause, cancel, conflict policy, verify, symlinks); Trash is `IOLib/Trash.cs`. Conflicts are asked from the worker thread via `AskConflict`. UI tests must answer or close every dialog they open (see `UiFlowTest.After`), or the waiting job keeps the busy lock for later tests. Tests redirect `Trash.MacTrashDir`/`LinuxTrashDir` and never touch the real Trash.
- **Panel → operations → dialogs:** `IOManager` (one panel: path, listing, selection, commands) delegates every file operation to its own `ProxyIO`. `ProxyIO` asks the user only through `IDialogService` (implemented by `View/DialogService`), so tests swap in `FakeDialogs`/`FakePanel` from `ComanderTest/Fakes.cs`.
- **Threading:**
  - `ProxyIO.RunBusy` runs work on `Task.Run`, guarded by one shared `StateMonitor`, so only one operation runs at a time across both panels.
  - Overwrite prompts are `allowOverride(name, path)` callbacks that `IOLib` calls on the worker thread. The dialog service marshals them to the UI thread and blocks the worker.
  - UI updates from workers go through `Dispatcher.UIThread.Post`.
  - `SyntaxParser` is shared by both panels and holds state, so `Perform` takes a lock.
- **Listing:** `IOManager.LoadFiles` lists on the thread pool and drops stale results with a `CancellationTokenSource`. `DirectoryManager` throws on unreadable folders (the panel logs "Cannot open X"). Search skips unreadable folders and reparse points instead.
- **Files vs folders:** `MetaDataFileFactory` (Comander) and `FileFactory` (IOLib) decide by `Directory.Exists`/attributes, never by extension. Files without an extension (`Makefile`) are files.
- **Messaging:** `WeakReferenceMessenger.Default` carries view signals. The message classes are in `Comander/Messages/` (focus, busy pulse/wait, refresh).
- **Plugins:** no MEF. `PluginManager` loads `Plugins/<Name>/<Name>.dll` and `Plugins/*.dll`, each in its own `PluginLoadContext` (`AssemblyLoadContext` + `AssemblyDependencyResolver`), and creates every public `ICommanderPlugin` class. `CommanderPlugin.dll` is always taken from the app, never from the plugin folder, or the interface types would not match. Bad files are logged and skipped. `SamplePlugins/Sha256Sums` is the sample plugin (not shipped in `Plugins/`); the author guide is `docs/PLUGINS.md`. If loading fails, `NullPluginManager` is used.
- **Config:** default `Config.xml`, `Commands.xml`, `Manuals/` and `Plugins/` sit next to the binary and are read from `AppContext.BaseDirectory`. The user `Config.xml` (panel paths, shortcuts) lives in the OS app-data folder and is written atomically on exit.

## Avalonia gotchas in this repo

- `AvaloniaUseCompiledBindingsByDefault` is false: bindings are reflection-based, so a typo in a binding path fails silently at runtime.
- `DataGridTextColumn` bindings are **two-way by default**. Always add `Mode=OneWay` to read-only columns. A two-way column once wrote back through private setters and reset `IsDirectory` on every row.
- **Keys and commands:** every shortcut lives in `Core/CommandRegistry.cs` (`AppCommand` + `KeyMap`). Don't add XAML `KeyBinding`s. Panel-scope keys run from `FilePane.axaml.cs` while a grid has focus; app-scope keys run from a tunnelling handler in `MainWindow.axaml.cs`. Menus show `{Binding KeyMap[id]}`. `KeyMapTest` fails if two commands share a key. Help → Keyboard shortcuts opens Settings on the Keyboard page.
- Each panel is `Comander/Controls/FilePane.axaml` (tabs, breadcrumb, list, footer). Its grid key bindings are run from a tunnelling `KeyDown` handler in `FilePane.axaml.cs`, because DataGrid swallows Enter/Tab/Home/Escape itself. Row state classes: `dir`, `marked`; the pane card gets `active`. The toolbar, menus and F-key bar bind to `MainVM.ActivePanel`.
- Colours, fonts and radii are tokens in `Themes/Dark.axaml` (`Brush.*`, `Font.UI`/`Font.Mono`, `Radius.*`), used only through `DynamicResource`. Don't hard-code colours in views. The visual spec is `docs/design/*.dc.html`.
- `Themes/Theme.axaml` has a global `TextBlock` style (Text.Primary). Text roles are classes (`muted`, `secondary`, `mono`, `title`, `warn`, `error`). Row styles must target `DataGridRow.<class> TextBlock`, not the row. Folder rows get the `dir` class in `LoadingRow`.
- Icons are stroke geometries in `Themes/Icons.axaml`: `<Path Classes="icon" Data="{StaticResource Icon.X}"/>`. The stroke follows the inherited `Foreground`.
- **Settings:** `Core/AppSettings.Current` (loaded in `App.CreateMainVm`, saved as `<Settings>` key/values in the user Config.xml). `Apply` rewrites the token brushes in the Dark theme dictionary, so everything on `DynamicResource` follows live. Converters read `AppSettings.Current`. UI tests must never press Done in the Settings window or toggle "Ask before moving to Trash": both write the user's real Config.xml.
- Window subclasses style as `Window`, so type selectors like `local|DialogWindow` never match. `DialogWindow` adds the `dialog` class for its card template.
- The DataGrid theme forces the cell `FocusVisual` visible, so it is hidden with `Opacity`, not `IsVisible`.
- `AVLN3001` is suppressed on purpose: dialogs take constructor arguments and the XAML designer loader is never used.

## Tests

NUnit 5 across `IOLibTest`, `IOLinqTest`, `ZipLibTest` and `ComanderTest`. Stay on NUnit: `[TestCase]` is used everywhere and `Avalonia.Headless.NUnit` drives the UI tests.

- `ComanderTest` UI tests use `[AvaloniaTest]` (setup in `HeadlessSetup.cs`). `PanelNavigationTest` sends real headless mouse and key input.
- `dotnet test` runs the assemblies in parallel. Each one uses its own temp root (`TestPaths.Root`) or a per-test GUID folder, so never share fixture paths between projects.
- `ComanderTest/BindingErrors.cs` fails any test that logs a binding error (a null link in a path is ignored), so a binding typo turns a UI test red.
- NUnit 5 async assertions must be awaited: `await Assert.ThatAsync(..., Throws...)`.
