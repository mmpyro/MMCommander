using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IOLib
{
    public interface IDriveManager
    {
        IEnumerable<DriveStruct> GetDrives();
    }

    public class DriveManager : IDriveManager
    {
        public IEnumerable<DriveStruct> GetDrives()
        {
            return System.IO.DriveInfo.GetDrives().Select(t => new DriveStruct(t.Name));
        }
    }

    public class DriveStruct
    {
        private readonly string driveLetter;

        public DriveStruct(string driveLetter)
        {
            this.driveLetter = driveLetter;
        }

        public override string ToString()
        {
            return driveLetter;
        }

        public string DriveLetter
        {
            get { return driveLetter; }
        }
    }
}