# MMCommander test profile

A profile tells Bogusz how to test one project: where its code lives, how to run each test layer, and what must never be run. Tags: `[found: file:line]` read from the project, `[guess]` inferred, none once a person has checked it.

## Roots

/Users/mmarszalek/playground/MMCommander

## Repos and test commands

| Repo / folder | Language | Scoped test command | Needs (Docker, services, setup) | Source |
|---|---|---|---|---|
| `.` (solution `MMComander.sln`, note spelling) | C# .NET 10, Avalonia 12 | All: `dotnet test MMComander.sln` (105 tests, ~10 s) | SDK 10.0.403 pinned (rolls forward). No Docker, no services | `[found: CLAUDE.md, global.json]` |
| `IOLibTest` (file ops) | C# NUnit 5 | `dotnet test IOLibTest --filter Name=JoinFile_Test` (one test); `--filter FullyQualifiedName~<Class>` (class) | none | `[found: CLAUDE.md]` |
| `ComanderTest` (VMs + headless UI) | C# NUnit 5 + Avalonia.Headless.NUnit | `dotnet test ComanderTest --filter FullyQualifiedName~ProxyIOTest` | none; headless, no display. Optional `MMC_SCREENSHOTS=<dir>` saves PNGs | `[found: CLAUDE.md, ComanderTest/WindowSmokeTest.cs]` |
| `IOLinqTest` (filter-language parser) | C# NUnit 5 | `dotnet test IOLinqTest --filter FullyQualifiedName~IOLinqTest` | none | ran 2026-11-10 |
| `ZipLibTest` (archives) | C# NUnit 5 | `dotnet test ZipLibTest --filter FullyQualifiedName~<Class>` | none | ran 2026-11-10 |
| Explicit 2.2 GB test | C# | `dotnet test IOLibTest --filter Name=Split_Over2GB_WritesAllParts` | about 2.2 GB of free disk in temp | `[found: IOLibTest/SplitJoinEdgeTest.cs]` |
| Build / lint | | `dotnet build MMComander.sln`. No linter. The only accepted warning is CS0067 in `LogLib/FileLogger.cs` | | `[found: CLAUDE.md]` |

There is no CI config, so these are the documented commands. `dotnet test` runs the 4 assemblies in parallel, and each one uses its own temp root (`TestPaths.Root`). On 2026-11-10 every test class passed when run alone, in all 4 projects.

## UI, performance and security entry points

| Kind | Entry point | Source |
|---|---|---|
| App (desktop UI, manual) | `dotnet run --project Comander` | `[found: CLAUDE.md]` |
| Headless UI tests | `dotnet test ComanderTest --filter FullyQualifiedName~PanelNavigationTest` (real headless mouse and key input) and `~WindowSmokeTest` (renders every window) | ran 2026-11-10 |
| Window screenshots | `MMC_SCREENSHOTS=<dir> dotnet test ComanderTest` | `[found: ComanderTest/WindowSmokeTest.cs]` |
| Load | none | |
| Scanners | `dotnet list package --vulnerable` | `[guess]` |

## Cross-repo / API consumers

Single repo. `CommanderPlugin` is the MEF plugin contract, and third-party DLLs load from `<app dir>/Plugins`, so a change to it can break external plugins `[guess]`. The formats of `Config.xml` and `Commands.xml` are a user-facing contract: user copies of them live in app data and next to the binary.

## E2E

none. The closest layer is the headless UI tests in `ComanderTest/` (`WindowSmokeTest.cs`, `PanelNavigationTest.cs`).

## Environments

Local machine only. Tests and the app run locally against temp folders. There are no remote environments.

## Forbidden commands

Ask first, every time:
- `git push`, and any `gh` command that writes (`gh issue create`, `gh pr create`).
- `dotnet test IOLibTest --filter Name=Split_Over2GB_WritesAllParts` (writes 2.2 GB to disk).

## Issue tracker

GitHub: `mmpyro/MMCommander` `[found: .git/config]`. No labels or templates found.

## Known flaky tests

None recorded. Only tested on macOS so far.

## Regression-prone areas

- Grid columns: `DataGridTextColumn` binds two-way by default, so read-only columns need `Mode=OneWay`. A two-way column once reset `IsDirectory`, and folders stopped opening.
- File vs folder classification must use `Directory.Exists` or the file attributes, never the extension. `Makefile` is a file.
- Folder-copy overwrite prompts: the `allowOverride` callback runs on a worker thread and blocks while the UI thread shows the dialog.
- Split/join of files over 2 GB needs long math. Only the `[Explicit]` test covers it.
- Test fixtures that rely on a folder another fixture created. `FileHelper` now creates parent folders; new tests should not assume `test_dir` exists.
- Shared state: one `SyntaxParser` (with a lock), and one `StateMonitor` busy lock across both panels.
- Bindings are reflection-based (`AvaloniaUseCompiledBindingsByDefault=false`), so a typo in a binding path fails silently.
- Stale listings: `IOManager.LoadFiles` cancels results that a newer navigation has replaced.
