using System;
using Avalonia;

namespace Comander
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args) =>
            AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
    }
}
