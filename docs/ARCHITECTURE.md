# Architecture

## Projects

```
Comander (WinExe, Avalonia)
├── IOLib            file/folder model, copy/move/split/join, search, drives
├── IOLinq           filter language parser (uses IOLib)
├── ZipAdapter       ZipLib: SharpZipLib to write, SharpCompress to extract
├── LogLib           ILogger: NLog file logger + GUI logger
└── CommanderPlugin  plugin contract (ICommanderPlugin, Response)

Tests: IOLibTest, IOLinqTest, ZipLibTest, ComanderTest (NUnit 5)
```

Shared build settings are in `Directory.Build.props`: net10.0, `LangVersion latest`, and implicit usings and nullable both off. `global.json` pins the SDK.

## Composition root

`App.CreateMainVm` (`Comander/App.axaml.cs`) builds the whole object graph by hand: about 12 lines. There is no DI container, on purpose:
- The two panels are two `IOManager` instances. They differ only by `ManagerType` and start path, and they point at each other through `SecondManager`.
- A container would need keyed registrations and factory lambdas, which is the same code plus a dependency.
- Tests build the real graph by calling `CreateMainVm` directly.

`App.OnFrameworkInitializationCompleted` wraps composition in a try/catch. If startup fails, it shows the exception in a window instead of crashing. It also hooks `Dispatcher.UIThread.UnhandledException`, which logs the error and lets the app continue.

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

## Threading

- Listing: `IOManager.LoadFiles` lists on the thread pool. A `CancellationTokenSource` drops results that a newer navigation has replaced. Typing in the filter box is debounced by 1 s.
- File operations: `ProxyIO.RunBusy` runs work on `Task.Run`.
  - `StateMonitor` lets one operation run at a time. A second operation logs "Another operation is in progress."
  - The lock is released in `finally`, even when the post-operation refresh throws.
- Overwrite prompts: `FileStructure.Copy` calls an `allowOverride(name, path)` callback on the worker thread. `ProxyIO` blocks on `IDialogService.AskOverride`, which marshals the dialog to the UI thread. "Override all" and "Cancel" are remembered for the rest of the batch.
- UI updates from workers use `Dispatcher.UIThread.Post`: `Notify`, `UnsetBusyApp` and the log list.
- `SyntaxParser` holds its parse queues as instance state. It is shared by both panels, so `Perform` takes a lock.

## Messaging

`WeakReferenceMessenger.Default` carries the view-level signals between view-models and views. The messages are classes in `Comander/Messages/`:
- `SetFocusMessage` and `FocusMessage`: focus a grid and row
- `PulseMessage` and `WaitMessage`: busy on and off
- `RefreshMessage`

## Errors

- Each item in a copy, move or delete batch has its own try/catch. A batch with failures ends with a log line such as "47 moved, 3 failed."
- Folder listing throws instead of returning an empty list. The panel then logs "Cannot open X: reason".
- Search skips folders it cannot read, and it skips reparse points (symlinks and junctions) to avoid loops.
- A broken `Commands.xml` or plugin is logged, and the app still starts.

## Plugins

Plugins are discovered with MEF (`System.ComponentModel.Composition`). At startup, `PluginManager` loads every `*.dll` in `<app folder>/Plugins`. A file that fails to load is logged and skipped.

A minimal plugin:

```csharp
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using CommanderPlugin;

[Export(typeof(ICommanderPlugin))]
[ExportMetadata("Name", "Count files")]   // shown in the F3 plugin list
public class CountFiles : ICommanderPlugin
{
    public Task<Response> InvokeAsync(string dir, IEnumerable<string> files)
    {
        // dir = current panel folder, files = selected full paths
        return Task.FromResult(new Response(OperationStatus.Success));
    }
}
```

Build it against `CommanderPlugin.csproj` (net10.0) and copy the DLL to `Plugins/`. The panels refresh after a plugin finishes. A `Response` with `Fail` status, or an exception, is logged.

## Tests

| Project | Covers |
|---|---|
| IOLibTest | Copy/move/delete/rename, folder copy conflicts, copy-into-self, split/join (one `[Explicit]` 2.2 GB test), search, factory |
| IOLinqTest | Filter language, and parallel `Perform` |
| ZipLibTest | Every format round-trips, AES, zip-slip rejection |
| ComanderTest | `ProxyIO` with fake dialogs and panel, `ConfigReader`, zip password rules, `LogMsg`, search VM, plugin loading, and a headless render of every window |

Conventions:
- Each assembly uses its own temp root (`TestPaths.Root`) or a per-test GUID folder, because `dotnet test` runs assemblies in parallel.
- Fakes live in `ComanderTest/Fakes.cs`: `FakeDialogs` answers dialogs, and `FakePanel` records log lines.
- `MMC_SCREENSHOTS=/some/dir dotnet test ComanderTest` saves a PNG of each window.
- To run the large split test: `dotnet test IOLibTest --filter Name=Split_Over2GB_WritesAllParts`.

The repo uses NUnit rather than xUnit: `[TestCase]` is used throughout, `Avalonia.Headless.NUnit` drives the UI tests, and xUnit's parallel-by-default classes would fight the file-system fixtures.
