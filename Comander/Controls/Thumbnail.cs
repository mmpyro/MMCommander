using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Comander.Controls
{
    // An Image that decodes FilePath at 160 px off the UI thread. Non-images and broken files show nothing
    // (the template shows the big file icon instead).
    public class Thumbnail : Image
    {
        public const int DecodeWidth = 160;
        private const int MaxCached = 400;
        private static readonly ConcurrentDictionary<string, Bitmap> Cache = new ConcurrentDictionary<string, Bitmap>();
        // Two decodes at a time, so a folder of photos doesn't flood the thread pool.
        private static readonly SemaphoreSlim Decoders = new SemaphoreSlim(2);
        private static readonly string[] Exts = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

        public static readonly StyledProperty<string> FilePathProperty = AvaloniaProperty.Register<Thumbnail, string>(nameof(FilePath));

        public static readonly DirectProperty<Thumbnail, bool> HasImageProperty =
            AvaloniaProperty.RegisterDirect<Thumbnail, bool>(nameof(HasImage), t => t.HasImage);

        private bool _hasImage;

        static Thumbnail()
        {
            FilePathProperty.Changed.AddClassHandler<Thumbnail>((t, _) => t.Load());
        }

        public string FilePath
        {
            get => GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public bool HasImage
        {
            get => _hasImage;
            private set => SetAndRaise(HasImageProperty, ref _hasImage, value);
        }

        public static bool IsImage(string path) => path != null && Array.IndexOf(Exts, Path.GetExtension(path).ToLowerInvariant()) >= 0;

        private async void Load()
        {
            var path = FilePath;
            Source = null;
            HasImage = false;
            if (!IsImage(path)) return;
            var bitmap = await Get(path);
            // Rows are recycled: only show it if this control still shows the same file.
            if (bitmap != null && path == FilePath)
            {
                Source = bitmap;
                HasImage = true;
            }
        }

        public static async Task<Bitmap> Get(string path)
        {
            if (Cache.TryGetValue(path, out var cached)) return cached;
            await Decoders.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        using var stream = File.OpenRead(path);
                        var bitmap = Bitmap.DecodeToWidth(stream, DecodeWidth);
                        // shortcut: whole-cache reset instead of LRU; fine for browsing, revisit if memory shows up in profiles.
                        if (Cache.Count >= MaxCached) Cache.Clear();
                        Cache[path] = bitmap;
                        return bitmap;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });
            }
            finally
            {
                Decoders.Release();
            }
        }
    }
}
