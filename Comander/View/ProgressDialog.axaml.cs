using System.ComponentModel;
using Comander.Core;

namespace Comander.View
{
    // Non-modal: "Run in background" only hides it; the status bar chip reopens it.
    public partial class ProgressDialog : DialogWindow
    {
        private readonly TransferJob _job;

        public ProgressDialog(TransferJob job)
        {
            InitializeComponent();
            _job = job;
            DataContext = job;
            BackgroundButton.Click += (_, _) => Close();
            job.PropertyChanged += OnJobChanged;
            Closed += (_, _) => job.PropertyChanged -= OnJobChanged;
            if (job.IsFinished) Avalonia.Threading.Dispatcher.UIThread.Post(Close);
        }

        private void OnJobChanged(object sender, PropertyChangedEventArgs e)
        {
            PauseText.Text = _job.IsPaused ? "Resume" : "Pause";
            if (_job.IsFinished)
                Avalonia.Threading.Dispatcher.UIThread.Post(Close);
        }
    }
}
