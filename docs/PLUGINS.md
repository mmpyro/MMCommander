# Writing plugins

A plugin is a .NET 10 class library. It gets the current panel folder and the selected files, does its work, and reports success or an error. The working sample is [`SamplePlugins/Sha256Sums`](../SamplePlugins/Sha256Sums/Sha256Sums.cs). It writes a `SHA256SUMS` file for the selected files.

## How a plugin works

1. You select files in a panel and press **Ctrl+P**. If nothing is marked, the file under the cursor is used.
2. The Plugins window lists every loaded plugin by its `Name`. Pick one and press **Enter**.
3. The app calls `InvokeAsync(dir, files)`:
   - `dir` is the panel's current folder;
   - `files` holds the full paths of the selection, files and folders alike.
4. The plugin returns a `Response`:
   - `Success` is logged as "Task: … completed";
   - `Fail` logs its `ErrorMessage`;
   - an exception is logged as an error.
5. Both panels refresh, so any files the plugin wrote show up at once.

A plugin has no UI of its own. Files it writes are how it shows results, and the status bar and **View log** show its outcome.

## 1. Create the project

```sh
dotnet new classlib -n MyPlugin -o MyPlugin
```

Reference the contract project. Use `Private="false"`: the app always supplies `CommanderPlugin.dll`, so a copy next to your plugin is never used.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="path/to/MMCommander/CommanderPlugin/CommanderPlugin.csproj" Private="false" />
  </ItemGroup>
</Project>
```

The contract isn't published as a NuGet package, so you build against the repo's project.

## 2. Implement `ICommanderPlugin`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using CommanderPlugin;

public class MyPlugin : ICommanderPlugin
{
    public string Name => "My plugin";   // shown in the Ctrl+P list; must be unique

    public Task<Response> InvokeAsync(string dir, IEnumerable<string> files) => Task.Run(() =>
    {
        // ... work on files ...
        return new Response(OperationStatus.Success);   // or: new Response("what went wrong")
    });
}
```

**Discovery rules:** every public, non-abstract class that implements `ICommanderPlugin` and has a parameterless constructor is created once, when the app starts. One DLL can hold several plugins.

**Threading rules:**
- `InvokeAsync` is called on the **UI thread**, so wrap real work in `Task.Run`, as the sample does. Otherwise the window freezes until the plugin finishes.
- Don't touch Avalonia controls from a plugin.

**Response:**

| Return | Result |
|---|---|
| `new Response(OperationStatus.Success)` | Info line "Task: <Name> completed succesfuly." |
| `new Response("message")` | `Fail` status; the message is logged as an error |
| throw | The exception is logged as an error |

## 3. Build and install

Each plugin goes in its own folder under `Plugins/` next to the app binary. **The DLL name must match the folder name:** `Plugins/MyPlugin/MyPlugin.dll`.

```sh
# published app (self-contained build)
dotnet publish MyPlugin -c Release -o <app folder>/Plugins/MyPlugin

# when you run from source with `dotnet run --project Comander`
dotnet publish MyPlugin -c Release -o Comander/bin/Debug/net10.0/Plugins/MyPlugin
```

Then restart the app. Plugins load only at startup.

Installing the sample works the same way:

```sh
dotnet publish SamplePlugins/Sha256Sums -c Release -o Comander/bin/Debug/net10.0/Plugins/Sha256Sums
```

A single `Plugins/MyPlugin.dll` with no folder also works, but only for a plugin with no dependencies of its own.

## 4. Dependencies

- Each plugin loads in its own `AssemblyLoadContext`. NuGet packages it uses go into its folder (`dotnet publish` copies them), and they may be different versions from the ones the app uses.
- An assembly that isn't in the plugin folder comes from the app: the .NET runtime, Avalonia, `CommanderPlugin`.
- Native libraries listed in the plugin's `.deps.json` (`runtimes/<rid>/native/`) are resolved as well.

## 5. Test a plugin

`InvokeAsync` is a plain method, so call it directly in a unit test:

```csharp
var response = await new MyPlugin().InvokeAsync(dir, new[] { file });
Assert.That(response.Status, Is.EqualTo(OperationStatus.Success));
```

`ComanderTest/SearchAndPluginTest.cs` (`Sha256Sums_*`) goes one step further. It installs the sample into a temp `Plugins/Sha256Sums/` folder, loads it with the real `PluginManager`, runs it, and checks `SHA256SUMS` against a known SHA-256 test vector.

## Limits

- **No UI.** A plugin can't show a window or ask the user anything.
- **Startup cost.** Each plugin is created once at startup, so keep constructors fast.
- **No unloading.** To pick up a rebuilt plugin, restart the app.
- **Duplicate names.** If two plugins share a `Name`, the first one loaded runs.
- **Full rights.** A plugin runs inside the app with your user's rights. Install only plugins you trust.

## Troubleshooting

| Symptom | Cause |
|---|---|
| Log shows "Cannot load plugin <path>" | The file isn't a .NET assembly, targets a newer .NET than the app (net10.0), or a type in it failed to load. The log file has the full exception (Help → Open log folder). |
| Plugin missing from the Ctrl+P list | The DLL name doesn't match its folder name; or the class isn't public, is abstract, or lacks a parameterless constructor; or the app wasn't restarted. |
| `Could not load file or assembly` when it runs | A dependency is missing from the plugin folder. Use `dotnet publish`, not `dotnet build`. |
