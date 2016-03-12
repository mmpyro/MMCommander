namespace Comander.Core
{
    public interface IStateMonitor
    {
        bool Enter();
        void Exit();
    }

    public class StateMonitor : IStateMonitor
    {
        private static bool _isBusy = false;
        private static object obj = new object();

        public bool Enter()
        {
            if (_isBusy == false)
            {
                lock (obj)
                {
                    if (_isBusy == false)
                    {
                        _isBusy = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public void Exit()
        {
            if (_isBusy)
            {
                lock (obj)
                {
                    if (_isBusy)
                        _isBusy = false;
                }
            }
        }
    }
}
