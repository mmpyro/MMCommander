using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Avalonia.Input;
using Comander.Core;
using Comander.ViewModel;
using NUnit.Framework;

namespace ComanderTest
{
    [TestFixture]
    public class KeyMapTest
    {
        [Test]
        public void Defaults_HaveNoConflicts()
        {
            var map = new KeyMap(mac: false);
            var owners = CommandRegistry.All.SelectMany(c => map.GesturesOf(c.Id).Select(g => (gesture: g.ToString(), c.Id)))
                .GroupBy(x => x.gesture).Where(g => g.Count() > 1).Select(g => g.Key + ": " + string.Join(", ", g.Select(x => x.Id)));
            Assert.That(owners, Is.Empty);
        }

        [Test]
        public void OnMac_CmdComesFirst_ButCtrlHAndCtrlTabStayCtrl()
        {
            var map = new KeyMap(mac: true);
            Assert.That(map["refresh"].ToString(), Is.EqualTo("Cmd+R"));
            Assert.That(map.GesturesOf("refresh").Select(g => g.ToString()), Is.EqualTo(new[] { "Cmd+R", "Ctrl+R" }), "Ctrl keeps working");
            Assert.That(map.GesturesOf("toggleHidden").Select(g => g.ToString()), Is.EqualTo(new[] { "Ctrl+H" }), "Cmd+H hides the app");
            Assert.That(map.GesturesOf("nextTab").Select(g => g.ToString()), Is.EqualTo(new[] { "Ctrl+Tab" }));
            Assert.That(map["copy"].ToString(), Is.EqualTo("F5"));
        }

        [Test]
        public void UserOverride_ReplacesOnlyThatCommand_AndUnknownIdsAreIgnored()
        {
            var map = new KeyMap(new Dictionary<string, string> { ["copy"] = "Ctrl+Shift+C", ["noSuchCommand"] = "F12" }, mac: false);
            Assert.That(map.GesturesOf("copy").Select(g => g.ToString()), Is.EqualTo(new[] { "Ctrl+Shift+C" }));
            Assert.That(map["move"].ToString(), Is.EqualTo("F6"));
            Assert.That(map.Overrides(), Is.EqualTo(new Dictionary<string, string> { ["copy"] = "Ctrl+Shift+C" }));
            map.Reset("copy");
            Assert.That(map.Overrides(), Is.Empty);
        }

        [Test]
        public void Find_MatchesExactModifiers()
        {
            var map = new KeyMap(mac: false);
            Assert.That(map.Find(new KeyEventArgs { Key = Key.F6 })?.Id, Is.EqualTo("move"));
            Assert.That(map.Find(new KeyEventArgs { Key = Key.F6, KeyModifiers = KeyModifiers.Shift })?.Id, Is.EqualTo("rename"));
            Assert.That(map.Find(new KeyEventArgs { Key = Key.K, KeyModifiers = KeyModifiers.Control }, c => c.Scope == CommandScope.Panel), Is.Null,
                "Ctrl+K is a window command, not a panel one");
        }

        // Manuals/keymap.html is hand-written; every default key must be documented there.
        [Test]
        public void KeymapManual_ListsEveryDefaultKey()
        {
            var html = WebUtility.HtmlDecode(System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Manuals", "keymap.html")));
            var missing = CommandRegistry.All.SelectMany(c => c.DefaultKeys.Select(k => (c.Id, key: k)))
                .Where(x => !html.Contains("<strong>" + x.key.Replace("Back", "Backspace").Replace("Escape", "Esc").Replace("OemComma", ",") + "</strong>"))
                .Select(x => x.Id + " " + x.key);
            Assert.That(missing, Is.Empty);
        }

        [TestCase("Cmd+K", true, "⌘K")]
        [TestCase("Shift+F8", true, "⇧F8")]
        [TestCase("Ctrl+Shift+T", true, "⌃⇧T")]
        [TestCase("Ctrl+K", false, "Ctrl+K")]
        public void KeyCapText_UsesMacSymbols(string gesture, bool mac, string expected)
        {
            Assert.That(Comander.Converters.GestureTextConverter.Format(KeyGesture.Parse(gesture), mac), Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Presets_HaveNoConflicts_AndDropSingleLetters(int preset)
        {
            var map = new KeyMap(mac: true);
            map.Load(SettingsWindowVM.PresetOverrides(preset, map));
            var owners = CommandRegistry.All.SelectMany(c => map.GesturesOf(c.Id).Select(g => (gesture: g.ToString(), c.Id)))
                .GroupBy(x => x.gesture).Where(g => g.Count() > 1).Select(g => g.Key);
            Assert.That(owners, Is.Empty);
            Assert.That(map.GesturesOf("copy").Select(g => g.ToString()), Is.EqualTo(new[] { "F5" }), "C is gone");
        }

        [TestCase(Comander.Core.DateFormatChoice.Iso, 0, "2026-10-20 16:57")]
        [TestCase(Comander.Core.DateFormatChoice.Relative, 21, "3 weeks ago")]
        [TestCase(Comander.Core.DateFormatChoice.Relative, 0, "just now")]
        [TestCase(Comander.Core.DateFormatChoice.Relative, 400, "1 year ago")]
        public void DateFormats(Comander.Core.DateFormatChoice format, int daysOld, string expected)
        {
            var date = new DateTime(2026, 10, 20, 16, 57, 0);
            Assert.That(Comander.Converters.DateConverter.Format(date, format, date.AddDays(daysOld)), Is.EqualTo(expected));
        }

        [TestCase("New tab", "new", 3)]
        [TestCase("Close tab", "tab", 2)]
        [TestCase("Compare directories", "cmpdir", 1)]
        [TestCase("Rename", "xyz", 0)]
        public void PaletteScore_PrefersPrefixThenSubstringThenFuzzy(string text, string query, int expected)
        {
            Assert.That(PaletteVM.Score(text, query), Is.EqualTo(expected));
        }
    }
}
