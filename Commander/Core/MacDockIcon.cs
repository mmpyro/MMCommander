using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Commander.Core
{
    // macOS takes the Dock icon from a .app bundle; started as a bare executable (dotnet run, the release
    // tarball) the Dock shows "exec". Set it at runtime: [NSApp setApplicationIconImage:[[NSImage alloc] initWithData:ico]].
    // shortcut: main.ico holds one 128 px image, so the Dock icon is soft on Retina; add a 512 px PNG if that matters.
    public static class MacDockIcon
    {
        private const string ObjC = "/usr/lib/libobjc.dylib";

        [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC)] private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC)] private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);
        [DllImport(ObjC)] private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg, ulong length);

        // Call on the UI thread, macOS only.
        public static void Apply()
        {
            byte[] bytes;
            using (var stream = AssetLoader.Open(new Uri("avares://Commander/Icons/main.ico")))
            using (var copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                bytes = copy.ToArray();
            }
            var buffer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                // dataWithBytes copies the buffer, so it can be freed right after.
                var data = objc_msgSend(objc_getClass("NSData"), sel_registerName("dataWithBytes:length:"), buffer, (ulong)bytes.Length);
                var image = objc_msgSend(objc_msgSend(objc_getClass("NSImage"), sel_registerName("alloc")), sel_registerName("initWithData:"), data);
                if (image == IntPtr.Zero)
                    throw new InvalidOperationException("NSImage cannot read Icons/main.ico");
                var app = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
                objc_msgSend(app, sel_registerName("setApplicationIconImage:"), image);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
