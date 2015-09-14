using System.Collections.Generic;

namespace Comander.Core
{
    public interface IShortcutManager
    {
        void Add(string value);
        void Remove(string value);
        IEnumerable<string> GetShortcuts();
    }

    public class ShortcutManager : IShortcutManager
    {
        readonly ISet<string> _shortcuts = new HashSet<string>();

        public void Add(string value)
        {
            _shortcuts.Add(value);
        }

        public void Remove(string value)
        {
            _shortcuts.Remove(value);
        }

        public IEnumerable<string> GetShortcuts()
        {
            return _shortcuts;
        } 
    }
}