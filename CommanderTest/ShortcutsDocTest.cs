using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Commander.Converters;
using Commander.Core;
using NUnit.Framework;

namespace CommanderTest
{
    // docs/shortcuts.md is generated from CommandRegistry, so the docs site never drifts from the app (#9).
    [TestFixture]
    public class ShortcutsDocTest
    {
        private const string Update = "MMC_UPDATE_DOCS=1 dotnet test CommanderTest --filter ShortcutsDocTest";

        [Test]
        public void ShortcutsPage_MatchesCommandRegistry()
        {
            var path = Path.Combine(RepoRoot(), "docs", "shortcuts.md");
            var expected = Render();
            if (Environment.GetEnvironmentVariable("MMC_UPDATE_DOCS") == "1")
                File.WriteAllText(path, expected);
            var actual = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : "";
            Assert.That(actual, Is.EqualTo(expected), "docs/shortcuts.md is stale; run " + Update);
        }

        [Test]
        public void Render_ListsEveryCommandWithItsKeys()
        {
            var text = Render();
            Assert.That(text, Does.Contain("| Copy to other pane | F5, C | F5, C |"));
            Assert.That(text, Does.Contain("| Command palette | Ctrl+K | ⌘K, ⌃K |"));
            Assert.That(text, Does.Contain("| Settings… | Ctrl+, | ⌘,, ⌃, |"));
            Assert.That(text, Does.Contain("| Check for updates | — | — |"));
            Assert.That(text, Does.Contain("| Parent folder | Backspace, Escape | ⌫, Esc |"));
            Assert.That(CommandRegistry.All.All(c => text.Contains("| " + c.Label + " |")), Is.True);
        }

        private static string Render()
        {
            var pc = new KeyMap(mac: false);
            var mac = new KeyMap(mac: true);
            var text = new StringBuilder();
            text.Append("<!-- Generated from Commander/Core/CommandRegistry.cs by CommanderTest/ShortcutsDocTest.cs. Do not edit: run ")
                .Append(Update).Append(" -->\n");
            text.Append("# Keyboard shortcuts\n\n");
            text.Append("These are the default keys. Change any of them in Settings → Keyboard (Help → Keyboard shortcuts), ")
                .Append("or in the user `Config.xml`, see [Settings](features/settings.md#keyboard).\n");
            text.Append("Commands without a key run from the [command palette](features/palette.md) (Ctrl+K, ⌘K on macOS).\n");
            foreach (var group in CommandRegistry.All.GroupBy(c => c.Group))
            {
                text.Append("\n## ").Append(group.Key).Append("\n\n");
                text.Append("| Action | Windows / Linux | macOS |\n|---|---|---|\n");
                foreach (var c in group)
                    text.Append("| ").Append(c.Label).Append(" | ").Append(Keys(pc, c.Id, false)).Append(" | ").Append(Keys(mac, c.Id, true)).Append(" |\n");
            }
            return text.ToString();
        }

        private static string Keys(KeyMap map, string id, bool mac)
        {
            // Avalonia names Enter "Return" and Backspace "Back"; the docs use the key-cap names.
            var keys = map.GesturesOf(id).Select(g => Regex.Replace(GestureTextConverter.Format(g, mac), @"\bReturn\b", "Enter"))
                .Select(k => Regex.Replace(k, @"\bBack\b", "Backspace")).ToList();
            return keys.Count == 0 ? "—" : string.Join(", ", keys);
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MMCommander.sln")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "MMCommander.sln not found above the test folder");
            return dir.FullName;
        }
    }
}
