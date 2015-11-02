
using System;
using System.Windows;
using Comander.ViewModel;
using Messanger;
using Comander.Core;
using Comander.Messages;

namespace Comander
{
    public partial class MainWindow : Window
    {
        private readonly Messanger.Messanger _messanger;

        public MainWindow()
        {
            InitializeComponent();
            ProgressWorker.Init();
            _messanger = Messanger.Messanger.GetInstance();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _messanger.Send(new WindowCloseEventArgs());
            ProgressWorker.DisposeWatcher();
        }

        private void Main_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _messanger.Send(new WindowPositionEventArgs(this,e));
        }
    }
}
