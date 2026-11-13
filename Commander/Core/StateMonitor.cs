using System.Threading;

namespace Commander.Core
{
    public interface IStateMonitor
    {
        bool Enter();
        void Exit();
    }

    // One shared instance (App.CreateMainVm) lets a single operation run at a time across both panels.
    public class StateMonitor : IStateMonitor
    {
        private int _busy;

        public bool Enter() => Interlocked.CompareExchange(ref _busy, 1, 0) == 0;

        public void Exit() => Interlocked.Exchange(ref _busy, 0);
    }
}
