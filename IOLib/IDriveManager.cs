using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IOLib
{
    public interface IDriveManager
    {
        IEnumerable<DriveStruct> GetDrives();
        // Free and total bytes of the volume holding path; null when it can't be read.
        DriveSpace GetSpace(string path);
    }

    public class DriveManager : IDriveManager
    {
        public IEnumerable<DriveStruct> GetDrives()
        {
            return DriveInfo.GetDrives().Select(t => new DriveStruct(t.Name, Label(t)));
        }

        public DriveSpace GetSpace(string path)
        {
            try
            {
                var full = Path.GetFullPath(path);
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                // The deepest mount point that contains the path (e.g. /Volumes/USB before /).
                var drive = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && full.StartsWith(d.RootDirectory.FullName, comparison))
                    .OrderByDescending(d => d.RootDirectory.FullName.Length)
                    .FirstOrDefault();
                return drive == null ? null : new DriveSpace(drive.AvailableFreeSpace, drive.TotalSize, drive.Name, Label(drive));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        private static string Label(DriveInfo drive)
        {
            try
            {
                return drive.IsReady && !string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.VolumeLabel : drive.Name;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return drive.Name;
            }
        }
    }

    public class DriveSpace
    {
        public DriveSpace(long free, long total, string root, string label)
        {
            Free = free;
            Total = total;
            Root = root;
            Label = label;
        }

        public long Free { get; }
        public long Total { get; }
        public string Root { get; }
        public string Label { get; }
        public double UsedFraction => Total > 0 ? 1d - (double)Free / Total : 0;
    }

    public class DriveStruct
    {
        private readonly string driveLetter;

        public DriveStruct(string driveLetter) : this(driveLetter, driveLetter)
        {
        }

        public DriveStruct(string driveLetter, string label)
        {
            this.driveLetter = driveLetter;
            Label = label;
        }

        public override string ToString()
        {
            return driveLetter;
        }

        public string DriveLetter
        {
            get { return driveLetter; }
        }

        // Volume name for display ("Macintosh HD"); falls back to the mount point.
        public string Label { get; }
    }
}
