using System.IO;
using IOLib;
using NUnit.Framework;

namespace IOLibTest
{
    [TestFixture]
    public class DriveManagerTest
    {
        // GetSpace now picks the mount with a path-boundary check; the temp folder must still find its volume.
        [Test]
        public void GetSpace_TempFolder_FindsAVolumeThatContainsIt()
        {
            var temp = Path.GetFullPath(Path.GetTempPath());
            var space = new DriveManager().GetSpace(temp);

            Assert.That(space, Is.Not.Null);
            Assert.That(temp, Does.StartWith(space.Root).IgnoreCase);
            Assert.That(space.Total, Is.GreaterThan(0));
        }
    }
}
