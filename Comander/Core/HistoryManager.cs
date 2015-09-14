using System.Collections.Generic;
using System.Linq;

namespace Comander.Core
{
    public class HistoryManager
    {
        IList<string> _history = new List<string>();
        private const int MaxHistorySize = 30;

        public void Add(string value)
        {
            if (!_history.Contains(value))
            {
                if (_history.Count < MaxHistorySize)
                    _history.Add(value);
                else
                {
                    _history = new List<string>(_history.Skip(1)){value};
                }
            }
        }

        public IEnumerable<string> GetHistory()
        {
            return _history;
        }   
    }
}