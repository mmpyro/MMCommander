using System.IO;
using IOLib;
using IOLibTest.helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace IOLibTest
{
    [TestFixture]
    public class FileCopyTest
    {
        private readonly string DestinationPath = Path.Combine(TestPaths.Root, "test_dir");

        [SetUp]
        public void Before()
        {
            FileHelper.InitCopyFileTest(DestinationPath);
        }

        [Test]
        public void FileCopy_Test()
        {
            //Given
            string dst1 = Path.Combine(FileHelper.Dir1(), FileHelper.FileName());
            string dst2 = Path.Combine(FileHelper.Dir2(), FileHelper.FileName());
            var fileFactory = new FileFactory();
            var file = fileFactory.CreateFileMsg(Path.Combine(DestinationPath, dst1));
            var dir = fileFactory.CreateFileMsg(Path.Combine(DestinationPath, FileHelper.Dir2()));
            //When
            file.OverrideCopy(dir);
            //Then
            ClassicAssert.IsTrue(FileHelper.Exist(Path.Combine(DestinationPath, dst2)));
            ClassicAssert.IsTrue(FileHelper.Exist(Path.Combine(DestinationPath, dst1)));
        }

        [TearDown]
        public void After()
        {
            FileHelper.CleanCopyFileTest(DestinationPath);
        }
    }
}