using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LogLib;

namespace Commander.Core
{
    public static class Shell
    {
        private static readonly HashSet<string> TerminalEditors = new HashSet<string> { "vi", "vim", "nvim", "nano", "micro", "hx", "kak", "joe", "ne", "mg" };

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
            foreach (var arg in Arguments(program, path))
                info.ArgumentList.Add(arg);
            Process.Start(info);
        }

        // Run, but a missing program or a launch error goes to the log instead of up the stack.
        public static void TryRun(ConfigurationProgram program, string path, string workingDirectory, ILogger logger)
        {
            try { Run(program, path, workingDirectory); }
            catch (Exception ex) { logger.Error(string.Format("Cannot start '{0}': {1}", program.Value, ex.Message)); }
        }

        public static List<string> Arguments(ConfigurationProgram program, string path)
        {
            return Split(program.Args)
                .Where(token => path != null || !token.Contains("{path}"))
                .Select(token => token.Replace("{path}", path))
                .ToList();
        }

        // Splits a command line on whitespace; "..." is one token without its quotes.
        public static List<string> Split(string line)
        {
            var tokens = new List<string>();
            var token = new StringBuilder();
            bool quoted = false, any = false;
            foreach (var c in line ?? "")
            {
                if (c == '"') { quoted = !quoted; any = true; }
                else if (char.IsWhiteSpace(c) && !quoted) { if (any) tokens.Add(token.ToString()); token.Clear(); any = false; }
                else { token.Append(c); any = true; }
            }
            if (any) tokens.Add(token.ToString());
            return tokens;
        }

        // A user command line such as `code {path}` or `"C:\Program Files\X\x.exe" {path}`.
        public static ConfigurationProgram Parse(string line)
        {
            var tokens = Split(line);
            return new ConfigurationProgram
            {
                Value = tokens.FirstOrDefault(),
                Args = string.Join(" ", tokens.Skip(1).Select(Quote)),
            };
        }

        // The user's command from Settings, or the default when it is blank.
        public static ConfigurationProgram Resolve(string userLine, ConfigurationProgram fallback) =>
            string.IsNullOrWhiteSpace(userLine) ? fallback : Parse(userLine);

        // Linux F4 default: $VISUAL, then $EDITOR, then Config.xml (xdg-open). Terminal editors get a terminal window.
        // shortcut: x-terminal-emulator exists only on Debian/Ubuntu; elsewhere the launch fails and is logged. Upgrade if users of other distros ask.
        public static ConfigurationProgram DefaultEditor(Func<string, string> env, ConfigurationProgram fallback)
        {
            var editor = new[] { env("VISUAL"), env("EDITOR") }.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
            if (editor == null)
                return fallback;
            var program = Parse(editor + " {path}");
            return TerminalEditors.Contains(Path.GetFileName(program.Value))
                ? Parse("x-terminal-emulator -e " + editor + " {path}")
                : program;
        }

        // F4 and F9 before the user's Settings override.
        public static ConfigurationProgram EditorDefault(IConfigReader config) =>
            OperatingSystem.IsLinux() ? DefaultEditor(Environment.GetEnvironmentVariable, config["notepad"]) : config["notepad"];

        public static ConfigurationProgram TerminalDefault(IConfigReader config) => config["cmd"];

        public static string Describe(ConfigurationProgram program) =>
            program == null ? "" : (Quote(program.Value) + " " + program.Args).Trim();

        private static string Quote(string token) => token != null && token.Any(char.IsWhiteSpace) ? "\"" + token + "\"" : token;

        public static void RunAsAdmin(string file, string workingDirectory)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Run as administrator is supported only on Windows");
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true, Verb = "runas", WorkingDirectory = workingDirectory });
        }
    }
}
