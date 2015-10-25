using System;
using System.Collections.Generic;

namespace Messanger
{
    public class Messanger : IMessanger
    {
        private readonly Dictionary<Type,List<Action<object>>> _dict;
        private static Messanger _instance;

        private Messanger()
        {
            _dict = new Dictionary<Type, List<Action<object>>>();
        }

        public static Messanger GetInstance()
        {
            return _instance ?? (_instance = new Messanger());
        }

        public void Register(Type type, Action<object> action)
        {
            List<Action<object>> list = null;
            if (!_dict.ContainsKey(type))
            {
                list = _dict[type] ?? new List<Action<object>>();
            }
            list.Add(action);
        }

        public void Send<T>(T message)
        {
            Type messageType = typeof (T);
            if (_dict.ContainsKey(messageType))
            {
                _dict[messageType].ForEach(t => t.Invoke(message));
            }
        }
    }
}
