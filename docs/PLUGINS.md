# Write a plugin

A plugin is a .NET 10 class library. It gets the current panel folder and the selected files, and it does its work. Then it reports success or an error. The working sample is [`SamplePlugins/Sha256Sums`](https://github.com/mmpyro/MMCommander/blob/master/SamplePlugins/Sha256Sums/Sha256Sums.cs). It writes a `SHA256SUMS` file for the selected files.

## How a plugin works

1. You select files in a panel and press **Ctrl+P**. If you mark nothing, the app uses the file under the cursor.
2. The Plugins window lists each loaded plugin by its `Name`. Select one and press **Enter**.
3. The app calls `InvokeAsync(dir, files)`:
   - `dir` is the current folder of the panel.
   - `files` holds the full paths of the selection. It includes both files and folders.
4. The plugin returns a `Response`:
   - For `Success`, the log shows "Task: … completed".
   - For `Fail`, the log shows its `ErrorMessage`.
   - For an exception, the log shows an error.
5. Both panels refresh, so you see at once the files that the plugin wrote.

A plugin has no UI of its own. It shows its results in the files that it writes. The status bar and **View log** show the outcome.

## 1. Create the project

```sh
dotnet new classlib -n MyPlugin -o MyPlugin
```

Add a reference to the contract project with `Private="false"`. The app always supplies `CommanderPlugin.dll`, so it never uses a copy next to your plugin.

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

The contract is not on NuGet, so you build against the project in the repo.

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

### Discovery

When the app starts, it creates one instance of each public, non-abstract class that implements `ICommanderPlugin` and has a parameterless constructor. One DLL can hold more than one plugin.

### Threads

- The app calls `InvokeAsync` on the **UI thread**. Put the real work in `Task.Run`, as the sample does. If you do not, the window freezes until the plugin finishes.
- Do not touch Avalonia controls from a plugin.

### Response

| Return | Result |
|---|---|
| `new Response(OperationStatus.Success)` | The log shows the info line "Task: <Name> completed succesfuly." |
| `new Response("message")` | The status is `Fail`, and the log shows the message as an error |
| throw | The log shows the exception as an error |

## 3. Build and install

Each plugin has its own folder under `Plugins/`, next to the app binary. **The DLL name must be the same as the folder name:** `Plugins/MyPlugin/MyPlugin.dll`.

```sh
# published app (self-contained build)
dotnet publish MyPlugin -c Release -o <app folder>/Plugins/MyPlugin

# when you run from source with `dotnet run --project Commander`
dotnet publish MyPlugin -c Release -o Commander/bin/Debug/net10.0/Plugins/MyPlugin
```

Then restart the app. The app loads plugins only when it starts.

You install the sample in the same way:

```sh
dotnet publish SamplePlugins/Sha256Sums -c Release -o Commander/bin/Debug/net10.0/Plugins/Sha256Sums
```

A single `Plugins/MyPlugin.dll` without a folder also works. Use it only for a plugin that has no dependencies of its own.

## 4. Dependencies

- Each plugin loads in its own `AssemblyLoadContext`. `dotnet publish` copies the NuGet packages of the plugin into its folder. Their versions can be different from the versions that the app uses.
- If an assembly is not in the plugin folder, the app supplies it. Examples are the .NET runtime, Avalonia and `CommanderPlugin`.
- The app also finds the native libraries that the `.deps.json` of the plugin lists (`runtimes/<rid>/native/`).

## 5. Test a plugin

`InvokeAsync` is a plain method, so you can call it in a unit test:

```csharp
var response = await new MyPlugin().InvokeAsync(dir, new[] { file });
Assert.That(response.Status, Is.EqualTo(OperationStatus.Success));
```

`CommanderTest/SearchAndPluginTest.cs` (`Sha256Sums_*`) does more. It installs the sample into a temp `Plugins/Sha256Sums/` folder and loads it with the real `PluginManager`. Then it runs the plugin and checks `SHA256SUMS` against a known SHA-256 test vector.

## Limits

- No UI. A plugin cannot show a window or ask the user a question.
- Startup cost. The app creates each plugin once when it starts, so keep the constructors fast.
- No unload. To use a plugin that you built again, restart the app.
- Duplicate names. If two plugins have the same `Name`, the app runs the plugin that it loaded first.
- Full rights. A plugin runs inside the app with the rights of your user account. Install only plugins that you trust.

## Troubleshooting

| Symptom | Cause |
|---|---|
| The log shows "Cannot load plugin <path>" | The file is not a .NET assembly, or it targets a newer .NET than the app (net10.0), or a type in it did not load. The log file has the full exception (Help → Open log folder). |
| The plugin is not in the Ctrl+P list | The DLL name is not the same as its folder name. Or the class is not public, is abstract, or has no parameterless constructor. Or you did not restart the app. |
| `Could not load file or assembly` when it runs | A dependency is not in the plugin folder. Use `dotnet publish`, not `dotnet build`. |
