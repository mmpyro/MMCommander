
using System;
using System.Windows;
using Comander.ViewModel;


namespace Comander
{
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();
            var eventResolver = Locator.MainWindowEventResolver;
            this.MouseMove += eventResolver.MainWindow_OnMouseMove;
            eventResolver.GetWindowsPositionAction = GetWindowPosition;
            ProgressWorker.Init();
        }

        private Point GetWindowPosition()
        {
            return new Point(this.Left, this.Top);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            var locator = new Locator();
            MainVM mainVm = locator.Main;
            mainVm.BeforeClose();
            ProgressWorker.DisposeWatcher();
        }
    }
}
