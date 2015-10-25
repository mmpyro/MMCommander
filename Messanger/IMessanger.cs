using System;

namespace Messanger
{
    public interface IMessanger
    {
        void Register(Type type, Action<object> action);
        void Send<T>(T message);
    }
}