using Comander.ViewModel;

namespace Comander.View
{
    public partial class CompareDirsWindow : DialogWindow
    {
        // startSync: "Sync dirs" opens the same window and goes straight to Synchronize (which still asks first).
        public CompareDirsWindow(CompareVM vm, bool startSync = false)
        {
            InitializeComponent();
            DataContext = vm;
            CloseButton.Click += (_, _) => Close();
            Opened += async (_, _) =>
            {
                await vm.Rescan();
                if (startSync && vm.SyncCommand.CanExecute(null))
                    await vm.Synchronize();
            };
        }
    }
}
