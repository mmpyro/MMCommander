using System;
using System.Collections.Generic;
using Commander.Core;
using LogLib;
using NUnit.Framework;

namespace CommanderTest
{
    // Editor (F4) and terminal (F9) command lines: Settings override, platform default, launch errors (#7, #8).
    [TestFixture]
    public class ShellTest
    {
        [TestCase("code {path}", new[] { "code", "{path}" })]
        [TestCase("  wt   -d {path}  --title x ", new[] { "wt", "-d", "{path}", "--title", "x" })]
        [TestCase("\"C:\\Program Files\\Notepad++\\notepad++.exe\" {path}", new[] { "C:\\Program Files\\Notepad++\\notepad++.exe", "{path}" })]
        [TestCase("term --title \"My shell\" {path}", new[] { "term", "--title", "My shell", "{path}" })]
        [TestCase("open -a Terminal", new[] { "open", "-a", "Terminal" })]
        [TestCase("", new string[0])]
        [TestCase("   ", new string[0])]
        public void Split_HonoursQuotes(string line, string[] expected)
        {
            Assert.That(Shell.Split(line), Is.EqualTo(expected));
        }

        [Test]
        public void Parse_QuotedProgramPath_KeepsSpacesInProgram()
        {
            var program = Shell.Parse("\"C:\\Program Files\\Notepad++\\notepad++.exe\" -n1 {path}");
            Assert.That(program.Value, Is.EqualTo("C:\\Program Files\\Notepad++\\notepad++.exe"));
            Assert.That(Shell.Arguments(program, "a.txt"), Is.EqualTo(new[] { "-n1", "a.txt" }));
        }

        [Test]
        public void Parse_QuotedArgument_StaysOneArgument()
        {
            var program = Shell.Parse("term --title \"My shell\" {path}");
            Assert.That(Shell.Arguments(program, "/tmp"), Is.EqualTo(new[] { "--title", "My shell", "/tmp" }));
        }

        [Test]
        public void Arguments_PathWithSpaces_IsOneArgument()
        {
            var program = Shell.Parse("gnome-terminal --working-directory={path}");
            Assert.That(Shell.Arguments(program, "/home/me/My Files"), Is.EqualTo(new[] { "--working-directory=/home/me/My Files" }));
            Assert.That(Shell.Arguments(Shell.Parse("wt -d {path} --title x"), "C:\\My Dir"), Is.EqualTo(new[] { "-d", "C:\\My Dir", "--title", "x" }));
        }

        [Test]
        public void Arguments_NoPath_DropsPathTokens()
        {
            var program = new ConfigurationProgram { Value = "open", Args = "-a Terminal {path}" };
            Assert.That(Shell.Arguments(program, null), Is.EqualTo(new[] { "-a", "Terminal" }));
        }

        [Test]
        public void Arguments_NoPathToken_PassesArgsOnly()
        {
            Assert.That(Shell.Arguments(Shell.Parse("x-terminal-emulator"), "/tmp"), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Resolve_BlankSetting_UsesDefault(string line)
        {
            var fallback = new ConfigurationProgram { Value = "notepad.exe", Args = "{path}" };
            Assert.That(Shell.Resolve(line, fallback), Is.SameAs(fallback));
        }

        [Test]
        public void Resolve_Setting_WinsOverDefault()
        {
            var program = Shell.Resolve("subl {path}", new ConfigurationProgram { Value = "notepad.exe" });
            Assert.That(program.Value, Is.EqualTo("subl"));
            Assert.That(Shell.Arguments(program, "a b.txt"), Is.EqualTo(new[] { "a b.txt" }));
        }

        private static readonly ConfigurationProgram XdgOpen = new ConfigurationProgram { Value = "xdg-open", Args = "{path}" };

        private static ConfigurationProgram Editor(string visual, string editor) =>
            Shell.DefaultEditor(name => new Dictionary<string, string> { ["VISUAL"] = visual, ["EDITOR"] = editor }[name], XdgOpen);

        [Test]
        public void DefaultEditor_Visual_WinsOverEditor()
        {
            var program = Editor("gedit", "nano");
            Assert.That(program.Value, Is.EqualTo("gedit"));
            Assert.That(Shell.Arguments(program, "f.txt"), Is.EqualTo(new[] { "f.txt" }));
        }

        [Test]
        public void DefaultEditor_OnlyEditor_IsUsed()
        {
            Assert.That(Editor(null, "kate").Value, Is.EqualTo("kate"));
            Assert.That(Editor("  ", "kate").Value, Is.EqualTo("kate"));
        }

        [Test]
        public void DefaultEditor_Neither_FallsBackToConfig()
        {
            Assert.That(Editor(null, null), Is.SameAs(XdgOpen));
        }

        [TestCase("vim")]
        [TestCase("/usr/bin/nano")]
        public void DefaultEditor_TerminalEditor_RunsInATerminal(string editor)
        {
            var program = Editor(null, editor);
            Assert.That(program.Value, Is.EqualTo("x-terminal-emulator"));
            Assert.That(Shell.Arguments(program, "my file.txt"), Is.EqualTo(new[] { "-e", editor, "my file.txt" }));
        }

        [Test]
        public void DefaultEditor_EditorWithArgs_KeepsThem()
        {
            var program = Editor(null, "code --wait");
            Assert.That(program.Value, Is.EqualTo("code"));
            Assert.That(Shell.Arguments(program, "f.txt"), Is.EqualTo(new[] { "--wait", "f.txt" }));
        }

        [Test]
        public void TryRun_MissingProgram_LogsAndDoesNotThrow()
        {
            var logger = new GUILogger();
            string message = null;
            logger.NotifyEvent += (_, m) => message = m;
            var program = Shell.Parse("mmc-no-such-editor-" + Guid.NewGuid().ToString("N") + " {path}");

            Assert.DoesNotThrow(() => Shell.TryRun(program, "f.txt", Environment.CurrentDirectory, logger));
            Assert.That(message, Does.Contain("Cannot start 'mmc-no-such-editor-"));
        }

        [Test]
        public void Describe_ShowsTheCommandLine()
        {
            Assert.That(Shell.Describe(new ConfigurationProgram { Value = "open", Args = "-t {path}" }), Is.EqualTo("open -t {path}"));
            Assert.That(Shell.Describe(Shell.Parse("\"C:\\Program Files\\x.exe\" {path}")), Is.EqualTo("\"C:\\Program Files\\x.exe\" {path}"));
            Assert.That(Shell.Describe(new ConfigurationProgram { Value = "cmd.exe" }), Is.EqualTo("cmd.exe"));
        }
    }
}
