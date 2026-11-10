using System;
using System.Diagnostics;

namespace Comander.Core
{
    public static class Shell
    {
        // Opens a file, folder or URL with the OS default handler.
        public static void Open(string target, string workingDirectory = null)
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = workingDirectory ?? "" });
        }

        // Runs a configured program. Each "{path}" token in its args becomes one argument, so paths with spaces stay intact;
        // without a path (Run menu) those tokens are dropped.
        public static void Run(ConfigurationProgram program, string path = null, string workingDirectory = null)
        {
            var info = new ProcessStartInfo(program.Value)
            {
                WorkingDirectory = workingDirectory ?? program.WorkingDir ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            foreach (var token in (program.Args ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (path != null || !token.Contains("{path}"))
                    info.ArgumentList.Add(token.Replace("{path}", path));
            Process.Start(info);
        }

        public static void RunAsAdmin(string file, string workingDirectory)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Run as administrator is supported only on Windows");
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true, Verb = "runas", WorkingDirectory = workingDirectory });
        }
    }
}
