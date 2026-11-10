using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Comander.View
{
    // UI-thread helpers for view-models: owner window, dialogs, pickers, clipboard. Safe to call from any thread.
    public static class WindowHost
    {
        // Headless tests have no desktop lifetime; the last MainWindow then owns the dialogs.
        public static Window Owner =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow ?? MainWindow.Last;

        // Modal dialogs dim the main window while they are open.
        public static Task<T> ShowDialog<T>(Func<Window> create) =>
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var owner = Owner;
                (owner as MainWindow)?.Dim(true);
                try { return await create().ShowDialog<T>(owner); }
                finally { (owner as MainWindow)?.Dim(false); }
            });

        public static Task ShowDialog(Func<Window> create) => ShowDialog<object>(create);

        public static void Show(Func<Window> create) =>
            Dispatcher.UIThread.Post(() => create().Show(Owner));

        public static Task<string> PickFolder(string startPath = null, TopLevel topLevel = null) =>
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var storage = (topLevel ?? Owner).StorageProvider;
                var start = string.IsNullOrEmpty(startPath) ? null : await storage.TryGetFolderFromPathAsync(startPath);
                var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { SuggestedStartLocation = start });
                return result.FirstOrDefault()?.TryGetLocalPath();
            });

        public static Task<string> PickFile(TopLevel topLevel = null) =>
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var result = await (topLevel ?? Owner).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions());
                return result.FirstOrDefault()?.TryGetLocalPath();
            });

        public static Task<IReadOnlyList<string>> GetClipboardFiles() =>
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var items = await Owner.Clipboard.TryGetFilesAsync();
                return (IReadOnlyList<string>)(items ?? new IStorageItem[0])
                    .Select(t => t.TryGetLocalPath()).Where(t => t != null).ToList();
            });

        public static Task SetClipboardFiles(IEnumerable<string> paths) =>
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var storage = Owner.StorageProvider;
                var items = new List<IStorageItem>();
                foreach (var path in paths)
                    items.Add((IStorageItem)await storage.TryGetFileFromPathAsync(path) ?? await storage.TryGetFolderFromPathAsync(path));
                await Owner.Clipboard.SetFilesAsync(items.Where(t => t != null));
            });
    }
}
