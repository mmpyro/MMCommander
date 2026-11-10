using System;
using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Comander.Converters
{
    public static class Icons
    {
        private static readonly ConcurrentDictionary<string, Bitmap> Cache = new ConcurrentDictionary<string, Bitmap>();

        public static Bitmap Load(string name) =>
            Cache.GetOrAdd(name, n => new Bitmap(AssetLoader.Open(new Uri("avares://Comander/Icons/" + n))));
    }
}
