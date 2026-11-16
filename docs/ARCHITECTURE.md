# Architecture

## Projects

```
Commander (WinExe, Avalonia)
├── IOLib            file/folder model, copy/move/split/join, search, drives
├── IOLinq           filter language parser (uses IOLib)
├── ZipAdapter       ZipLib: SharpZipLib to write, SharpCompress to extract
├── LogLib           ILogger: NLog file logger + GUI logger
└── CommanderPlugin  plugin contract (ICommanderPlugin, Response)

Sample: SamplePlugins/Sha256Sums (a plugin; references only CommanderPlugin)
Tests: IOLibTest, IOLinqTest, ZipLibTest, CommanderTest (NUnit 5)
```

`Directory.Build.props` holds the build settings for all projects: net10.0, `LangVersion latest`, and implicit usings and nullable both off. `global.json` pins the SDK.

## Composition root

`App.CreateMainVm` (`Commander/App.axaml.cs`) builds the full object graph by hand in about 12 lines. The app has no DI container, for these reasons:
- The two panels are two `IOManager` instances. Only their `ManagerType` and start path are different. Each one points at the other through `SecondManager`.
- A container needs keyed registrations and factory lambdas. That is the same code, plus a dependency.
- Tests call `CreateMainVm` directly to build the real graph.

`App.OnFrameworkInitializationCompleted` puts the composition in a try/catch. If the startup fails, it shows the exception in a window and does not crash. It also adds a handler to `Dispatcher.UIThread.UnhandledException`. The handler logs the error, and the app continues.

## Main types

| Type | Role |
|---|---|
| `MainVM` | Window view-model: menus, logs, split/join, search, busy state |
| `IOManager` (+ `IOManagerDefinition`) | One file panel: path, listing, selection, and the panel's commands |
| `ProxyIO` (`IProxyIO`) | All file operations for a panel. It asks the user through `IDialogService` |
| `IDialogService` → `View/DialogService` | Every question to the user (confirm, text, override, zip, unzip, clipboard). It is the only place that opens dialogs |
| `WindowHost` | Owner window, `ShowDialog`, file/folder pickers, clipboard |
| `StateMonitor` (`IStateMonitor`) | A single busy lock shared by both panels |
| `ConfigReader` | Default config next to the binary, user config in app data |
| `MetaDataFileFactory` | Turns paths into `IMetadataFileStructure` (a file or a folder, decided by file attributes) |
| `FileDiffVM` → `View/FileDiffWindow` | Compare files (Shift+F3). `IOLib/TextDiff` makes the aligned line diff (lines unique on both sides are anchors, as in patience diff; Myers aligns the gaps between them) and the in-line character spans (Myers). `IOLib/TextDoc` loads a file as lines, so that saving an untouched file writes the same bytes back. Every edit is a line replace, followed by a full re-diff. Binary files get a read-only hex view |

## Threading

- Listing: `IOManager.LoadFiles` lists the folder on the thread pool. A `CancellationTokenSource` drops the results of an older navigation. The filter box waits 1 s after the last key press (debounce).
- File operations: `ProxyIO.RunBusy` runs the work on `Task.Run`.
  - `StateMonitor` lets only one operation run at a time. A second operation logs "Another operation is in progress."
  - A `finally` block releases the lock, also when the refresh after the operation throws.
- Overwrite prompts: `FileStructure.Copy` calls an `allowOverride(name, path)` callback on the worker thread. `ProxyIO` waits on `IDialogService.AskOverride`, which shows the dialog on the UI thread. `ProxyIO` remembers "Override all" and "Cancel" for the rest of the batch.
- Workers update the UI through `Dispatcher.UIThread.Post`: `Notify`, `UnsetBusyApp` and the log list.
- `SyntaxParser` keeps its parse queues as instance state. Both panels share one parser, so `Perform` takes a lock.

## Messaging

`WeakReferenceMessenger.Default` carries the view signals between view-models and views. Each message is a class in `Commander/Messages/`:
- `SetFocusMessage` and `FocusMessage`: focus a grid and row
- `PulseMessage` and `WaitMessage`: busy on and off
- `RefreshMessage`

## Errors

- Each item in a copy, move or delete batch has its own try/catch. If items fail, the batch ends with a log line such as "47 moved, 3 failed."
- If a folder listing fails, it throws. It does not return an empty list. The panel then logs "Cannot open X: reason".
- Search skips folders that it cannot read. It also skips reparse points (symlinks and junctions), so it does not loop.
- The app logs a broken `Commands.xml` or plugin and still starts.

## Plugins

At startup, `PluginManager` loads `<app folder>/Plugins/<Name>/<Name>.dll` (a plugin with its own dependencies) and each single `Plugins/*.dll`. Each plugin gets its own `AssemblyLoadContext`, so its dependencies can be different from those of the app. `CommanderPlugin.dll` always comes from the app. The manager creates one instance of each public class that implements `ICommanderPlugin` and has a parameterless constructor. If a file does not load, the manager logs it and skips it.

How to write, install and test a plugin: [PLUGINS.md](PLUGINS.md). The sample is `SamplePlugins/Sha256Sums`.

## Tests

| Project | Covers |
|---|---|
| IOLibTest | Copy/move/delete/rename, folder copy conflicts, copy-into-self, split/join (one `[Explicit]` 2.2 GB test), search, factory, text diff and the byte-exact `TextDoc` round trip |
| IOLinqTest | Filter language, and parallel `Perform` |
| ZipLibTest | Every format round-trips, AES, zip-slip rejection |
| CommanderTest | `ProxyIO` with fake dialogs and panel, `ConfigReader`, zip password rules, `LogMsg`, search VM, plugin loading, and a headless render of every window |

Conventions:
- Each assembly uses its own temp root (`TestPaths.Root`) or a per-test GUID folder, because `dotnet test` runs assemblies in parallel.
- Fakes live in `CommanderTest/Fakes.cs`: `FakeDialogs` answers dialogs, and `FakePanel` records log lines.
- `MMC_SCREENSHOTS=/some/dir dotnet test CommanderTest` saves a PNG of each window.
- To run the large split test: `dotnet test IOLibTest --filter Name=Split_Over2GB_WritesAllParts`.

The repo uses NUnit, not xUnit, for three reasons:
- The tests use `[TestCase]` everywhere.
- `Avalonia.Headless.NUnit` drives the UI tests.
- xUnit runs test classes in parallel by default, and that conflicts with the file-system fixtures.
