using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace IOLib
{
    // Moves files to the OS trash instead of deleting them.
    // Windows: Recycle Bin via SHFileOperation. macOS: ~/.Trash. Linux: freedesktop.org trash (~/.local/share/Trash).
    public static class Trash
    {
        // Overridable for tests; null means the user's real trash.
        public static string MacTrashDir { get; set; }
        public static string LinuxTrashDir { get; set; }

        /// <summary>Why the path can't go to the trash, or null when it can.</summary>
        public static string Unavailable(string path)
        {
            if (OperatingSystem.IsWindows())
                return null;
            var dir = OperatingSystem.IsMacOS() ? MacDir() : LinuxDir();
            if (dir == null)
                return "No Trash folder for this user";
            // A rename only works inside one volume; files elsewhere would have to be copied, so refuse instead.
            var space = new DriveManager();
            var trashRoot = space.GetSpace(Directory.Exists(dir) ? dir : Path.GetDirectoryName(dir))?.Root;
            var itemRoot = space.GetSpace(path)?.Root;
            if (trashRoot != null && itemRoot != null && trashRoot != itemRoot)
                return "It is on another volume (" + itemRoot + ") than the Trash";
            return null;
        }

        public static void MoveToTrash(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new FileNotFoundException("Not found", path);
            if (OperatingSystem.IsWindows())
                Recycle(path);
            else if (OperatingSystem.IsMacOS())
                MoveInto(path, MacDir(), null);
            else
                MoveInto(path, Path.Combine(LinuxDir(), "files"), Path.Combine(LinuxDir(), "info"));
        }

        private static string MacDir() =>
            MacTrashDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".Trash");

        private static string LinuxDir()
        {
            if (LinuxTrashDir != null) return LinuxTrashDir;
            var data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(data))
                data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            return Path.Combine(data, "Trash");
        }

        // shortcut: macOS ~/.Trash move has no Finder "Put Back"; use NSFileManager.trashItem via ObjC interop if that's wanted.
        private static void MoveInto(string path, string filesDir, string infoDir)
        {
            Directory.CreateDirectory(filesDir);
            var full = Path.GetFullPath(Path.TrimEndingDirectorySeparator(path));
            var target = Path.Combine(filesDir, Path.GetFileName(full));
            if (File.Exists(target) || Directory.Exists(target))
                target = TransferEngine.UniqueName(target);
            if (infoDir != null)
            {
                Directory.CreateDirectory(infoDir);
                var info = new StringBuilder()
                    .Append("[Trash Info]\n")
                    .Append("Path=").Append(Uri.EscapeDataString(full).Replace("%2F", "/")).Append('\n')
                    .Append("DeletionDate=").Append(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)).Append('\n');
                File.WriteAllText(Path.Combine(infoDir, Path.GetFileName(target) + ".trashinfo"), info.ToString());
            }
            if (Directory.Exists(full)) Directory.Move(full, target); else File.Move(full, target);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

        // shortcut: untested here (macOS dev box); verify on Windows that items land in the Recycle Bin.
        private static void Recycle(string path)
        {
            const uint FO_DELETE = 3;
            const ushort FOF_ALLOWUNDO = 0x40, FOF_NOCONFIRMATION = 0x10, FOF_SILENT = 0x4, FOF_NOERRORUI = 0x400;
            var op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = Path.GetFullPath(path) + "\0\0",
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
            };
            int code = SHFileOperation(ref op);
            if (code != 0 || op.fAnyOperationsAborted)
                throw new IOException(string.Format("Recycle Bin refused {0} (code {1})", path, code));
        }
    }
}
