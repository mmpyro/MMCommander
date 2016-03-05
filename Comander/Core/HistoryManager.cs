using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Comander.Core
{
    public interface IHistoryManager
    {
        void Add(string value);
        IEnumerable<string> GetHistory();
    }

    public class HistoryManager : IHistoryManager
    {
        IList<string> _history = new List<string>();
        private const int MaxHistorySize = 30;

        public void Add(string value)
        {
            if ( IsVaidPath(value) && !_history.Contains(value))
            {
                if (_history.Count < MaxHistorySize)
                    _history.Add(value);
                else
                {
                    _history = new List<string>(_history.Skip(1)){value};
                }
            }
        }

        private bool IsVaidPath(string value)
        {
            return Directory.Exists(value);
        }

        public IEnumerable<string> GetHistory()
        {
            return _history;
        }   
    }
}