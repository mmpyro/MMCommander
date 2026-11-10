using System;
using System.IO;

namespace ZipLibTest
{
    // Unique root per test assembly: dotnet test runs assemblies in parallel.
    public static class TestPaths
    {
        public static readonly string Root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "mmc-ZipLibTest-" + Guid.NewGuid().ToString("N"))).FullName;
    }
}
