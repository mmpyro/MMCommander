using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace IOLib
{
    public enum CompareMode
    {
        NameSizeDate,
        Content,
        Name,
    }

    public enum CompareKind
    {
        OnlyLeft,
        OnlyRight,
        NewerLeft,
        NewerRight,
        // Same date, different size or content: needs a human, never synced automatically.
        Different,
        Identical,
    }

    public class CompareSide
    {
        public string FullName { get; init; }
        public bool IsDirectory { get; init; }
        public long Size { get; init; }
        public DateTime Modified { get; init; }
    }

    public class CompareRow
    {
        // Path below the compared folders, "/"-free on the top level ("notes", "notes/todo.txt").
        public string RelativePath { get; init; }
        public string Name => Path.GetFileName(RelativePath);
        public bool IsDirectory => (Left ?? Right).IsDirectory;
        public CompareSide Left { get; init; }
        public CompareSide Right { get; init; }
        public CompareKind Kind { get; init; }
    }

    // Compares two folders entry by entry (docs/design/CompareDirs.dc.html).
    public static class DirComparer
    {
        // FAT and network shares store times with 2 s precision.
        private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(2);

        public static List<CompareRow> Compare(string left, string right, CompareMode mode, bool recursive, CancellationToken token = default)
        {
            var rows = new List<CompareRow>();
            Walk(left, right, string.Empty, mode, recursive, rows, token);
            return rows;
        }

        private static void Walk(string left, string right, string prefix, CompareMode mode, bool recursive, List<CompareRow> rows, CancellationToken token)
        {
            var l = Entries(left);
            var r = Entries(right);
            var names = l.Keys.Union(r.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => !(l.TryGetValue(n, out var a) ? a : r[n]).IsDirectory)
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                token.ThrowIfCancellationRequested();
                l.TryGetValue(name, out var ls);
                r.TryGetValue(name, out var rs);
                var relative = prefix.Length == 0 ? name : prefix + "/" + name;
                rows.Add(new CompareRow { RelativePath = relative, Left = ls, Right = rs, Kind = Kind(ls, rs, mode) });
                if (recursive && ls != null && rs != null && ls.IsDirectory && rs.IsDirectory)
                    Walk(ls.FullName, rs.FullName, relative, mode, true, rows, token);
            }
        }

        private static Dictionary<string, CompareSide> Entries(string dir)
        {
            var result = new Dictionary<string, CompareSide>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(dir)) return result;
            foreach (var info in new DirectoryInfo(dir).EnumerateFileSystemInfos())
            {
                var isDir = info is DirectoryInfo;
                result[info.Name] = new CompareSide
                {
                    FullName = info.FullName,
                    IsDirectory = isDir,
                    Size = isDir ? 0 : ((FileInfo)info).Length,
                    Modified = info.LastWriteTime,
                };
            }
            return result;
        }

        public static CompareKind Kind(CompareSide left, CompareSide right, CompareMode mode)
        {
            if (right == null) return CompareKind.OnlyLeft;
            if (left == null) return CompareKind.OnlyRight;
            if (left.IsDirectory != right.IsDirectory) return CompareKind.Different;
            if (mode == CompareMode.Name || left.IsDirectory) return CompareKind.Identical;
            var newer = left.Modified - right.Modified;
            bool sameTime = newer.Duration() <= Tolerance;
            bool sameData = mode == CompareMode.Content
                ? left.Size == right.Size && TransferEngine.SameHash(left.FullName, right.FullName)
                : left.Size == right.Size && sameTime;
            if (sameData) return CompareKind.Identical;
            if (sameTime) return CompareKind.Different;
            return newer > TimeSpan.Zero ? CompareKind.NewerLeft : CompareKind.NewerRight;
        }
    }
}
