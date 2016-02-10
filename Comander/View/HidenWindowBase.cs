using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Comander.View
{
    public class HidenWindowBase : Window
    {
        protected CancellationTokenSource ts = new CancellationTokenSource();
        protected CancellationToken ct;
        protected Task _task;

        protected void InitTask()
        {
            ct = ts.Token;
        }

        public void FinishTask()
        {
            ts.Cancel();
        }

        public new void MouseEnter()
        {
            if (_task.Status == TaskStatus.Running)
            {
                ts.Cancel();
                Debug.WriteLine("Enter");
            }
        }

        public new void MouseLeave()
        {
            Debug.WriteLine("Leave");
            _task = new Task(() =>
            {
                var startTime = DateTime.Now;
                while (true)
                {
                    Thread.Sleep(100);
                    if (ct.IsCancellationRequested)
                    {
                        ts = new CancellationTokenSource();
                        ct = ts.Token;
                        break;
                    }
                    else if( (DateTime.Now - startTime).TotalSeconds >= 1.5  )
                    {
                        Application.Current.Dispatcher.Invoke(this.Close);
                        break;
                    }
                }
            });
            _task.Start();
        }
    }
}