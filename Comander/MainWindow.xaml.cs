using System;
using System.Windows;
using Comander.Messages;

namespace Comander
{
    public partial class MainWindow : Window
    {
        private readonly Messanger.Messanger _messanger;

        public MainWindow()
        {
            InitializeComponent();
            _messanger = Messanger.Messanger.GetInstance();
            _messanger.Register(typeof(PulseMessage), _ => ProgressWorker.Pulse());
            _messanger.Register(typeof(WaitMessage), _ => ProgressWorker.Wait());
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _messanger.Send(new WindowCloseEventArgs());
            ProgressWorker.Dispose();
        }

        private void Main_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _messanger.Send(new WindowPositionEventArgs(this,e));
        }
    }
}
