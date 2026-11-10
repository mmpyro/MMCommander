# MMCommander UI redesign: "Graphite Classic" (Direction A)

## Status (2026-11-10)
- All 8 phases are implemented. Nothing is committed.
- Build: only CS0067.
- Tests, all green: IOLinq 19, ZipLib 17, IOLib 71, Comander 98.
- Deviations from this plan are listed in CLAUDE.md and in the hand-off notes.

## Context
- **Goal:** move the app from today's ad-hoc dark UI to the design in `~/Downloads/commander-design.zip`, Direction A ("Graphite Classic").
  - Today's UI: #181818 background, yellow folder rows, bitmap toolbar icons, two toolbar rows per panel, and one red status line.
  - The design source: `Main.dc.html`, `Tokens.dc.html`, `CopyFlow`, `DeleteDialog`, `CompareDirs`, `Settings`, `Shortcuts`.
  - `Sidebar` and `Inspector` are alternatives B and C. They are reference only and won't be built.
- **Rules from the brief:**
  - Keep existing behaviour and view-model logic.
  - Prefer restyling and restructuring XAML.
  - Delete no feature without asking.
  - No NuGet package without asking.
  - Commit nothing.
  - Each phase builds, runs, and ends with a pause for review.
- **User decisions (2026-11-10):**
  - **Fonts:** bundle the Inter and JetBrains Mono TTFs (OFL licence). No NuGet.
  - **Keymap:** move only the keys that clash with the new F-key bar. The single-letter keys stay as aliases.
  - **Extras, all four in scope:** folder tabs, Move to Trash, Thumbnail view, Ctrl+K palette.
  - **Dropped:** the selected-file name box, the per-panel CurrentOperation box, the log `<` `>` step buttons and the ToolBar toggle. Each one's function moves to the status bar, the progress dialog or the log panel.
- **Step 0 of implementation:**
  - Unzip the design into `docs/design/`. The brief says it lives there; today it is only in Downloads.
  - Copy this plan to `docs/redesign-plan.md`, which the brief asks for. In plan mode, only this plan file may be written.

## Current architecture (what the redesign sits on)
- **Stack:** .NET 10, Avalonia 12.1.6, FluentTheme (default density), Avalonia.Controls.DataGrid 12.1.2 and CommunityToolkit.Mvvm 8.4.2.
  - Bindings are reflection-based (`AvaloniaUseCompiledBindingsByDefault=false`), so a typo fails silently.
  - Nullable and implicit usings are off.
- **Composition:** `App.CreateMainVm` builds `MainVM` and two `IOManager`s (`Io1`/`Io2`, linked through `SecondManager`). Each manager has its own `ProxyIO` and `HistoryManager`, and they share one `StateMonitor` (one operation at a time).
- **Main window:** `Comander/MainWindow.axaml` has four rows.
  - Menu: Options, Run, Ext, Tools, Help.
  - Two `View/CommandWindow` controls: drive ComboBox, path TextBox, selected-file box, two toolbar rows, filter.
  - Two `Controls/IOGrid` controls: a DataGrid plus a CurrentOperation box.
  - Status bar: busy bar, icon, "n of N", message, Logs flyout, `<` `>`.
- **Keys:** the 27 `KeyBindings` in `Controls/IOGrid.axaml:24-51` are run by a tunnelling KeyDown handler (`IOGrid.axaml.cs:40-45`). Nothing else is bound, so there's no window-level key map.
- **Focus:** `IOGrid.GotFocus` sends a `FocusMessage(GridName)`, which sets `_focus` on the matching manager. Tab runs `SwitchFocusCommand`, which sends a `SetFocusMessage`.
- **Styling:**
  - `Themes/Theme.axaml` holds the `MainBorderBrush` #0092BE and `BackgroundBrush` #181818 brushes, a global white `TextBlock`, yellow `DataGridRow.dir` rows and a blue hover.
  - Hard-coded colours elsewhere: Red in `ShortcutsWindow.axaml:7` and `ZipWindow.axaml:21`.
  - `MainBorderBrush` is used in 5 files.
  - 11 bitmap toolbar icons in `CommandWindow.axaml`, plus tick, log and tree bitmaps via `Converters/Icons.cs`.
- **Dialogs:** `View/DialogWindow` is a borderless, centred window with a 2 px border, closed by Esc. `View/WindowHost` and `View/DialogService` show them.
  - Override, Confirm, Input, About, Zip, UnZip, Split, Join, Search, Tree, History, Shortcuts, Info, Plugin.
- **File operations:**
  - `ProxyIO.CopyFile/MoveFile/DeleteFile` run through `RunBusy`.
  - Conflicts go through an `allowOverride(name, path)` callback that blocks the worker on `OverrideWindow`. `CopyOption` is None, OverrideAll, OverrideSingle or Cancel, where "Cancel" means skip the rest.
  - **Missing today:** progress bytes, cancellation, keep-both, Trash. Delete is permanent, and Move throws on a conflict.
- **Data:**
  - `FileMetaDataStructure` already has `LastModifyTime`, but the grid shows `CreationTime` as a raw `ToString()`.
  - Size is shown as whole KB.
  - `DriveManager` returns drive names only, with no free space.
  - `HistoryManager` is a de-duplicated list of up to 30 paths, with no position, so there's no back or forward.
- **Config:** `ConfigReader` persists programs, shortcuts and the two panel paths. There's no theme or key-binding section.
- **"Index was out of range" in the status bar:** `MainVM.NextLog` (`ViewModel/MainVM.cs:160-167`) reads `_logsCollection[_current]` when the list is empty.
  - It fires when you click `>` before any message, or after "Clear logs".
  - `BackLog` is guarded.
  - The global handler (`App.axaml.cs:39-43`) logs the error, so it shows as the first message.
  - A second, unconfirmed candidate is the DataGrid `SelectedItem ??= message.SelectedFile ?? items[0]` running against a replaced `Files` collection (`IOGrid.axaml.cs:70`). Confirming it needs the stack trace from the log file.

## Mapping: current UI to new design
| Current | New home |
|---|---|
| Menu Options: Switch, Source = Target, Reload drives, Open logs, Clear logs | Go: Swap panes, Same path in other pane. View: Reload drives. Help: Open log folder. The log panel gets "Clear". |
| Menu Run (Programs), Ext (Exts) | Tools ▸ Run ▸ …, Tools ▸ Open with ▸ … |
| Menu Tools: Compare Dir, Split, Join, Search | Toolbar Compare (new dialog, phase 6). File ▸ Split / Join. Tools ▸ Search. The Ctrl+K palette searches too. |
| Menu Help: Keymap, Syntax, About | Help (same three), plus Settings ▸ Keyboard |
| Panel toolbar row 1: Root, Choose dir, Parent, History, Shortcuts, `.*` | Go menu: Root, Choose folder…, History…, Bookmarks…. Toolbar: Parent, and the Show-hidden eye. |
| Panel toolbar row 2: Delete, Move, Copy, New dir, New file, Paste | F-key bar F8/F6/F5/F7. Toolbar: New folder. File ▸ New file. Edit ▸ Paste. |
| Drive ComboBox + path TextBox | Drive picker button with a flyout, plus a breadcrumb. Clicking empty breadcrumb space or pressing Ctrl+L switches it to the editable path TextBox, so typed paths still work. |
| Selected-file box, CurrentOperation box | Dropped. The status bar shows the cursor's full path; operations show in the status bar and the progress dialog. |
| Filter TextBox | Filter box in the pane's path row, placeholder "Filter  ext:png" |
| Grid columns: tick, Name, Ext, Creation Date, Type, Size [KB] | icon, Name, Ext, Size (auto units, "—" for folders), Modified (`LastModifyTime`, yyyy-MM-dd HH:mm). Type is dropped because the icon shows it. The tick becomes the "marked" row state. Creation time stays in the Info window. |
| Status bar: busy bar, icon, "n of N", message, Logs, `<` `>` | Status dot + Ready/Busy, cursor path in mono, and an error chip ("N errors · View log") that opens the existing log list |
| `OverrideWindow` | Conflict dialog (phase 5) |
| Delete confirm (`ConfirmWindow`) | Delete dialog with Trash or permanent (phase 5). ConfirmWindow stays for other prompts. |
| Tools ▸ Compare Dir (`FileMarker`, red mark that nothing displays) | Compare directories dialog (phase 6). It replaces FileMarker. |

## Keymap (decided: move only the clashes)
| Action | New key | Old key, status |
|---|---|---|
| View / open with default app | F3 | E, kept |
| Edit (notepad) | F4 | N, kept |
| Copy to other pane | F5 | C, kept |
| Move | F6 | M, kept |
| New folder | F7 | F1, kept |
| Delete (Trash from phase 5) | F8 | Delete, kept |
| Delete permanently | Shift+F8, Shift+Delete | new (phase 5) |
| Terminal here | F9 | Shift+C, kept |
| Rename | Shift+F6, F2 | Ctrl+R, **moved** |
| New file | Shift+F4 | F2, **moved** |
| Refresh | Ctrl+R | F5, **moved** |
| Plugins | Ctrl+P | F3, **moved** |
| Parent folder | Backspace | Esc, kept |
| Mark | Space, Insert | S, kept |
| Mark all | Ctrl+A | A, kept |
| New tab / close tab | Ctrl+T / Ctrl+W | new |
| Tree window | Ctrl+Shift+T | Ctrl+T, **moved** |
| Back / Forward | Alt+Left / Alt+Right | new |
| Command palette | Ctrl+K | new |
| Compare directories | Shift+F2 | new |
| Settings | Ctrl+, | new |
| Unchanged | Enter, Tab, Home, Ctrl+H, Ctrl+I, Ctrl+D, Ctrl+Z, Ctrl+U, Ctrl+C/V, R, T, Ctrl+E | |

- **macOS:** Ctrl shortcuts map to ⌘ through Avalonia's `PlatformHotkeyConfiguration`, and the labels show ⌘.
- **Swap panes:** stays menu-only, because the design's ⌘U clashes with Unzip.

## Files to add or change
- **Add:**
  - `Comander/Themes/Dark.axaml`: tokens as `Color` plus `SolidColorBrush`. ThemeDictionaries cover Dark, plus Black in phase 7.
  - `Themes/Icons.axaml`: `StreamGeometry` paths copied from the design SVGs.
  - `Fonts/*.ttf`: Inter 400/500/600 and JetBrains Mono 400/500.
  - `Controls/FilePane.axaml(.cs)`: tabs, path row, list and footer.
  - `Converters/` additions: date, file-type brush and auto size.
  - `Core/CommandRegistry.cs`: the one map of commands and gestures.
  - `View/CommandPalette.axaml`.
  - `View/CopyDialog`, `ProgressDialog`, `ConflictDialog`, `DeleteDialog`, `CompareDirsWindow`, `SettingsWindow` (with Appearance and Keyboard pages).
  - `Core/JobQueue.cs`.
  - `IOLib/Trash.cs`, `IOLib/DirComparer.cs`.
  - Progress, cancel and policy support in IOLib copy.
- **Change:**
  - `App.axaml`: merge the new dictionaries, `FluentTheme DensityStyle="Compact"`, fonts.
  - `Themes/Theme.axaml`: rewritten on tokens.
  - `MainWindow.axaml(.cs)`, `IOGrid.axaml(.cs)` (merged into FilePane), `CommandWindow.axaml` (removed in phase 3).
  - `IOManager*.cs`: `IsActive`, tabs, back/forward, counts, free space.
  - `MainVM.cs`: `ActivePanel`, error count, the `NextLog` fix.
  - `ProxyIO.cs`, `DialogService.cs`, `DialogWindow.cs`, `ConfigReader.cs` (Appearance and KeyBindings sections, still reading old files).
  - `IOLib/IDriveManager.cs`: free and total space.
  - `Manuals/keymap.html`, `README.md`, `docs/ARCHITECTURE.md`.
  - Tests in `ComanderTest`.
- **Remove after replacement:**
  - Toolbar bitmaps in `Icons/` (keep `main.ico`).
  - `OverrideWindow`, `CopyOption`/`CopyOptionDto`, `FileMarker`, the log `<` `>` commands.

## Phases
Every phase ends with these checks:
- `dotnet build MMComander.sln`, with only CS0067 allowed.
- `dotnet test MMComander.sln`, all green.
- `MMC_SCREENSHOTS=~/.bogusz/ui/mmcommander-redesign dotnet test ComanderTest`, then compare the PNGs against the `.dc.html` values.
- A summary, then a pause for your check.

### Phase 1: tokens, fonts and icons; restyle with no layout change
1. Add `Themes/Dark.axaml` with every token from the brief, plus `Accent.Tint` (#5B9DFF33), `Accent.Marked` (#5B9DFF14), `Text.Marked` #8AB8FF and `Cursor.Inactive` #1F242C. Add brushes named `Brush.<token>`, use them through `DynamicResource`, and merge the dictionary in `App.axaml`.
2. Add the fonts (download the official OFL releases and add each licence file). Add the `Font.UI` and `Font.Mono` FontFamily resources. The Window style sets Inter 13.
3. Set `FluentTheme DensityStyle="Compact"`. Rewrite `Theme.axaml`:
   - Window background is `Bg.Base`.
   - Text is `Text.Primary`.
   - Menu, TextBox and ComboBox use `Bg.Input`/`Bg.Raised`, radius 6. Buttons use radius 8.
   - DataGrid header: 11 px, 600 weight, uppercase, `Text.Muted`.
   - Rows are 28 px with a `Bg.Raised` hover. Folder rows use weight 500 (no more yellow).
   - Replace the hard-coded Red and `MainBorderBrush` uses, then delete the two old brushes.
4. Add `Themes/Icons.axaml`: about 25 Lucide-style paths taken from the design SVGs.
   - The icons are **stroked**, so they render as `Path` with `Stroke`, `StrokeThickness=1.8`, round caps and `Stretch=Uniform` in a 16 px box, not `PathIcon`, which fills.
   - Add one `Path.icon` style.
   - Swap the 11 bitmap buttons in `CommandWindow` for these icons. The layout stays the same.
5. Converters:
   - `FileSizeConverter`: auto B/KB/MB/GB, and "—" for folders.
   - New `DateConverter`: `yyyy-MM-dd HH:mm`.
   - New `FileKindBrushConverter`: folder uses the Accent; image, archive, config and data extensions get their own colour; anything else gets the "other" colour.
   - Each converter gets a small `[TestCase]` test.
6. Dialog base (`DialogWindow`): `Bg.Dialog`, radius 14, `Border.Control`, and the title style 17 px / 600.
7. Binding-error guard: the headless tests attach an Avalonia log sink, so a Binding-area warning fails the test. This catches silent typos during the restyle.

### Phase 2: main window shell (menu bar, single toolbar, F-key bar, status bar)
1. `MainVM.ActivePanel` (IOManager) is updated from the existing `FocusMessage`. Each `IOManager` gets an observable `IsActive`.
2. Menu bar (40 px):
   - On the left, the app mark plus "Commander".
   - File, Edit, View, Go, Tools, Plugins, Help, holding every item from the mapping table. Each binds to `ActivePanel.X` or a MainVM command.
3. Toolbar (48 px, `Bg.Toolbar`):
   - Parent, Refresh, New folder, Rename, Terminal here and the Show-hidden eye (the eye inverts `HideHidden`, and the test lookup changes).
   - Back/Forward come in phase 3, Compare/Sync in phase 6, the view toggle in phase 8 and the search button in phase 4. Each button is added in the phase that makes it work.
4. F-key bar: 7 equal buttons with mono Accent key labels. F8 uses the Danger colour. They bind to `ActivePanel` commands. The F-keys from the keymap table are added to the IOGrid bindings in this phase.
5. Status bar (28 px, `Bg.Chrome`):
   - A Success-coloured dot with "Ready", or a Warning dot with "Busy · {CurrentOperation}".
   - The cursor's full path in mono.
   - An error chip in Danger colours: `ErrorCount` counts errors since the log was last opened, and clicking opens the existing `LogList` flyout plus a Clear button. It is hidden when the count is 0.
   - `<` `>` and `LogCount` are removed.
6. `CommandWindow` loses both toolbar rows and the selected-file box. IOGrid loses the CurrentOperation box.
7. Update `keymap.html`, the README shortcut table and the `UiFlowTest` lookups (the `HiddenToggle` and `StatusMessage` names move).

### Phase 3: the FilePane control (tabs, path row, list, row states, footer, active pane)
1. `Controls/FilePane.axaml` replaces CommandWindow plus IOGrid, keeping `DataGrid1`, the tunnelling key handler and the GridName focus logic.
   - It is a card: `Bg.Pane`, radius 10.
   - The border is the Accent at 60% when `IsActive`, otherwise `Border.Subtle`.
   - Clicking anywhere in the pane activates it.
2. **Tabs:**
   - A new `PaneTab` model holds the path and its back/forward stacks. IOManager gets `Tabs`, `ActiveTab`, and New/Close/Next tab commands.
   - Switching tab sets `ActualPath`. `EvaluatePath` pushes to the active tab's back stack.
   - The tab strip follows the design: the active tab merges into the pane, and there's a "+" button.
   - Tabs are saved in Config.xml, and old files that hold a single path still load.
   - The toolbar gets Back and Forward.
3. **Path row:**
   - The drive picker is a button showing the drive icon, name and chevron. Its flyout lists `Drives`.
   - The breadcrumb is an `ItemsControl` of path segments in mono on `Bg.Input`; clicking a segment navigates there.
   - Edit mode: click empty space or press Ctrl+L to show the TextBox, which commits on Enter and goes back on Esc.
   - The filter box sits on the right.
4. **List:** stays a DataGrid, which is already virtualized, resizable, supports multi-select and is used by the tests.
   - Columns: icon (Path geometry plus the kind brush), Name, Ext (mono, muted), Size (right-aligned, mono), Modified (mono, muted).
   - The sticky header comes from the DataGrid.
   - Folders stay first; check `FileNameComparer`.
5. **Row states:**
   - The cursor is the DataGrid's selected row: the `FilePane.active DataGridRow:selected` style uses `Accent.Tint` plus a 1 px inset accent border on the template's BackgroundRectangle. In an inactive pane it is `Cursor.Inactive` with no border.
   - Marked rows have the `marked` class, set in `LoadingRow` and kept up to date through `MetaData.PropertyChanged`. They use `Text.Marked` and `Accent.Marked`.
   - Hover uses `Bg.Raised`.
6. **Footer:**
   - Folder and file counts.
   - The marked count and size.
   - "X free of Y" plus an 80 px bar, from a new `IDriveManager.GetSpace(path)` that wraps `DriveInfo`.
7. Update the UI tests that depend on the tick Image and the CommandWindow TextBox order, and add tests for tabs, back/forward and the active-pane class.

### Phase 4: central command and key map, errors, Ctrl+K palette
1. **`Core/CommandRegistry`:** one list of `AppCommand { Id, Label, Group, DefaultGestures, Resolve(MainVM) → ICommand }`.
   - User overrides come from the `<KeyBindings>` section of Config.xml.
   - The pane's key handler looks up the gesture in the registry and runs the command on `ActivePanel`. All `<KeyBinding>` entries in the XAML are removed.
   - Menu items show their gesture from the registry as `InputGesture`.
   - A test checks that every default gesture appears in `keymap.html`.
2. **Errors:**
   - First write a red test for `NextLog` on an empty list, then fix it.
   - Read your log file for an "Index was out of range" stack trace. If it points at the DataGrid candidate, reset `SelectedItem` when `Files` is swapped and add a test.
   - The rule that errors only go to the log and the chip is checked by test: no dialog opens on an error.
3. **Palette:**
   - An overlay popup opens on Ctrl+K or from the menu-bar search button ("Search files or run a command ⌘K").
   - It holds a TextBox and a list that fuzzy-matches registry labels and the active pane's file names.
   - Enter runs a command or jumps to a file, and Esc closes it.

### Phase 5: Copy, Move and Delete dialogs, job queue, Trash
1. **IOLib copy engine:**
   - Chunked stream copy (1 MB) reports bytes through `IProgress` and supports a `CancellationToken` and a pause gate.
   - The `allowOverride` bool becomes `Func<Conflict, ConflictDecision>`, where the decision is Replace, Skip or KeepBoth (naming "name (1).ext"), with an apply-to-all flag.
   - Policies: Ask, Overwrite older, Skip, Keep both.
   - Preserve timestamps is applied explicitly after the stream copy. Verify after copy compares SHA-256 hashes of source and target.
   - Follow symlinks: when off, the link itself is copied. Only matching filter applies to the marked items.
   - Move uses the same conflict policy, so it no longer throws.
   - Each of these gets an IOLibTest.
2. **`Core/JobQueue`:** FIFO, one job at a time, so today's single-operation rule stays. It holds the `StateMonitor` while a job runs.
   - Jobs: Copy, Move, Delete, Trash.
   - Progress is sent to the UI thread at no more than 10 Hz.
3. **Dialogs:** the brief calls for overlays on a dimmed window. To keep `WindowHost` and the dialog tests, I'll keep owner-centred borderless windows and add a dim layer over MainWindow while one is open.
   - **CopyDialog** (F5/F6): title and summary, destination (other pane, mono) with Browse and free space, the policy segment, the four checkboxes, Cancel / Add to queue / Copy ↵. Shift+F5 copies within the same pane using Keep both.
   - **ProgressDialog:** "Copying 2 of 3", a file bar, a total bar, Speed / Remaining / Errors tiles, and Run in background / Pause / Cancel. In the background, the status bar shows a job chip.
   - **ConflictDialog** replaces OverrideWindow: Existing and Incoming cards (an image thumbnail when the file is an image), "Do this for all", and Compare / Skip / Keep both / Replace.
   - **DeleteDialog:** the item list with sizes, and a Trash or Permanent segment. Permanent turns the border `#5A2F2B` and shows the warning. Shift+F8 opens it in permanent mode. Esc cancels and Enter confirms. It includes the "Don't ask again when moving to Trash" checkbox, which is saved.
4. **`IOLib/Trash`:**
   - Windows: `Microsoft.VisualBasic.FileIO.FileSystem` with `RecycleOption`, which is in-box.
   - macOS: move into `~/.Trash` with a unique name.
   - Linux: freedesktop `~/.local/share/Trash/{files,info}`.
   - If the item is on another volume, or the Trash can't be written, the dialog switches to permanent mode and shows a warning, so it never deletes silently.
5. Delete `OverrideWindow` and `CopyOption`, and update `FakeDialogs` and `ProxyIOTest`.

### Phase 6: Compare directories
1. **`IOLib/DirComparer`** is a pure function: (left listing, right listing, mode, recursive) gives rows of kind OnlyLeft, OnlyRight, NewerLeft, NewerRight, Different or Identical.
   - Modes: name+size+date, content (SHA-256) and name only.
   - It gets table-driven tests.
2. **`CompareDirsWindow`:**
   - A header with both paths.
   - Aligned rows with the centre status column: → is blue, ← is amber, ≠ means different, = means identical and is dimmed.
   - Filter chips with counts, "Compare by", Include subfolders and Rescan.
   - The footer shows the plan summary and a "Synchronize…" button, which queues → and ← copies on the JobQueue. ≠ rows are listed for review and never synced automatically.
3. The toolbar gets Compare and Sync dirs; Sync opens the same window and runs Synchronize. Shift+F2 opens it. `FileMarker` is removed.

### Phase 7: Settings (Appearance, Keyboard)
1. **`SettingsWindow`:**
   - Left nav. Only sections with content are shown: General (start paths, confirm settings), Appearance, Keyboard, File operations (default conflict policy, Trash default) and Plugins (the loaded list).
   - Panes & columns, File types and Advanced appear only once they have content.
   - Opened with Ctrl+, or View ▸ Settings.
2. **Appearance**, saved in the `<Appearance>` section of Config.xml and applied live:
   - Theme: Dark, Black (OLED: a custom `ThemeVariant` with Dark as its parent, and pure black surfaces) or Follow system.
   - The accent swatches rewrite `Accent`, `Accent.Tint` and `Accent.Marked` in the app resources.
   - Row density sets the `RowHeight` resource to 28 or 34.
   - UI and mono fonts: the bundled fonts are always present; Cascadia Code and SF Mono are offered only when installed.
   - Date format: ISO, locale or relative. Size units: auto, KB or bytes. Both are read by the converters, and the grid refreshes when they change.
   - Toggles:
     - Show hidden is the default for new panes; Ctrl+H stays per pane.
     - Calculate folder sizes runs in the background on Space.
     - Colour icons by type.
     - Separate Ext column: when off, the name includes the extension.
   - A live preview list.
3. **Keyboard**, editing the CommandRegistry overrides:
   - Presets: "Total Commander (modified)" (the defaults), Total Commander and Finder-like.
   - Search by command or key, the four groups, and key-cap chips.
   - Click to record, and Backspace to clear.
   - A conflict warning with Reassign / Cancel.
   - A "Changed only · N" filter, Export… (a file you save), and Reset preset.

### Phase 8: Thumbnail view
1. The toolbar's segmented Details/Thumbnail toggle is per pane, through an `IOManager.ViewMode` property.
2. Thumbnail mode swaps the DataGrid for a `ListBox` with a `WrapPanel` over the same `Files`. The cursor, marking, keys and the active state stay the same.
3. Images are decoded at 160 px, off the UI thread, with a size-limited cache. Other files show the large kind icon.

## Risks
- **Silent bindings:** reflection bindings won't catch typos at compile time. The phase 1 log-sink guard fails the tests instead.
- **Templated DataGrid styles:** `/template/ Rectangle#BackgroundRectangle` and the header template depend on DataGrid 12.1.2's Fluent template. Pin the version, and screenshot the row states in tests.
- **UI tests tied to structure:**
  - `UiFlowTest.cs:76` (grid order)
  - `UiFlowTest.cs:106-112` (tick Image, CommandWindow TextBox order)
  - `UiFlowTest.cs:309` and `UiFlowTest.cs:320-331` (`HiddenToggle`, `StatusMessage`, `LogsButton`)
  - `PanelNavigationTest.cs:47` (`dir` row class)
  - Each phase updates them deliberately, without weakening the assertions.
- **Dialog code-behind uses named controls** (Zip, UnZip, Split, Join, Confirm, Input). Restyle them only; keep the names.
- **Platform-specific code:**
  - Trash on 3 OSes. On macOS, `~/.Trash` loses Finder's "Put Back", and recent macOS may need Full Disk Access.
  - The terminal commands per OS.
  - ⌘ vs Ctrl labels.
  - Phase 5 will be tested only on macOS here; Windows and Linux stay unverified.
- **Config.xml schema:** new sections must not break reading old user files. A test loads a phase-0 file.
- **Thumbnail WrapPanel isn't virtualized:** large folders are slow. `shortcut:` comment in the code; upgrade to a virtualizing panel if it hurts.
- **Spec gaps:**
  - `Tokens.dc` gives a comfortable row height of 32; the brief and `Settings.dc` give 34. I'll use 34.
  - The design uses #8B93A1 for some text, which is dimmer than `Text.Muted`. Per your rule, all text uses `Text.Muted` #9AA2AE or brighter, and dimmer greys are used only for non-text glyphs.
- **Scope:** phases 5–8 are real new features and much larger than 1–4. Expect each to need its own review cycle.

## Verification (each phase, and at the end)
1. `dotnet build MMComander.sln`: 0 errors, only CS0067.
2. `dotnet test MMComander.sln`: all green. Never run the `[Explicit]` 2.2 GB split test.
3. Run with `MMC_SCREENSHOTS=~/.bogusz/ui/mmcommander-redesign` and compare against the `.dc.html` inline values: colours, sizes, radius, spacing. Screenshots are never committed.
4. `dotnet run --project Comander` by hand: Tab between panes, F5 copy with a conflict, F8 to Trash, Ctrl+K, a settings accent change applied live.
5. No commits until you say so.
