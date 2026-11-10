using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Comander.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IOLib;

namespace Comander.Core
{
    public enum JobState
    {
        Queued,
        Running,
        Done,
        Cancelled,
    }

    // One queued copy/move/delete with live progress for the progress dialog and the status bar chip.
    public class TransferJob : ObservableObject, IProgress<TransferProgress>
    {
        private readonly Action<TransferJob> _work;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly object _gate = new object();
        private TransferProgress _latest;
        private long _lastPost;
        private JobState _state = JobState.Queued;

        public TransferJob(string verb, string destination, Action<TransferJob> work)
        {
            Verb = verb;
            Destination = destination;
            _work = work;
            PauseCommand = new RelayCommand(TogglePause);
            CancelCommand = new RelayCommand(Cancel);
        }

        public string Verb { get; }
        public string Destination { get; }
        public PauseGate Pause { get; } = new PauseGate();
        public CancellationToken Token => _cts.Token;
        public TaskCompletionSource Completion { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        public RelayCommand PauseCommand { get; }
        public RelayCommand CancelCommand { get; }

        public JobState State
        {
            get => _state;
            private set
            {
                if (SetProperty(ref _state, value))
                    OnPropertyChanged(nameof(IsFinished));
            }
        }

        public bool IsFinished => _state is JobState.Done or JobState.Cancelled;
        public bool IsPaused => Pause.IsPaused;

        // Filled from TransferProgress at most 10 times a second.
        public string Title { get; private set; } = string.Empty;
        public string CurrentName { get; private set; } = string.Empty;
        public double FileFraction { get; private set; }
        public double TotalFraction { get; private set; }
        public string FileText { get; private set; } = string.Empty;
        public string TotalText { get; private set; } = string.Empty;
        public string SpeedText { get; private set; } = "—";
        public string RemainingText { get; private set; } = "—";
        public int Errors { get; private set; }
        public string ChipText => Verb + " " + (int)Math.Round(TotalFraction * 100) + "%";

        public void Cancel()
        {
            Pause.Resume();
            _cts.Cancel();
        }

        private void TogglePause()
        {
            if (Pause.IsPaused) Pause.Resume(); else Pause.Pause();
            OnPropertyChanged(nameof(IsPaused));
        }

        public void AddError()
        {
            lock (_gate) Errors++;
            Post(force: true);
        }

        public void Report(TransferProgress value)
        {
            lock (_gate) _latest = value;
            Post(force: false);
        }

        // Cancelled while still queued.
        internal void Skip()
        {
            State = JobState.Cancelled;
            Completion.TrySetResult();
        }

        // Called by the queue on the worker thread.
        internal void Run()
        {
            State = JobState.Running;
            _clock.Start();
            Exception error = null;
            try
            {
                _work(this);
            }
            catch (Exception ex)
            {
                // Surfaces in whoever awaits Completion (the command), which logs it like any other failure.
                error = ex;
            }
            finally
            {
                _clock.Stop();
                Post(force: true);
                State = _cts.IsCancellationRequested ? JobState.Cancelled : JobState.Done;
                if (error != null) Completion.TrySetException(error); else Completion.TrySetResult();
            }
        }

        private void Post(bool force)
        {
            var now = _clock.ElapsedMilliseconds;
            if (!force && now - Interlocked.Read(ref _lastPost) < 100) return;
            Interlocked.Exchange(ref _lastPost, now);
            Dispatcher.UIThread.Post(Apply);
        }

        private void Apply()
        {
            TransferProgress p;
            lock (_gate) p = _latest;
            if (p != null)
            {
                Title = string.Format("{0} {1} of {2}", Verb, Math.Max(1, p.FileIndex), p.FileCount);
                CurrentName = p.CurrentName ?? string.Empty;
                FileFraction = p.FileTotal > 0 ? (double)p.FileBytes / p.FileTotal : 1;
                TotalFraction = p.TotalBytes > 0 ? (double)p.Bytes / p.TotalBytes : (p.FileCount > 0 ? (double)p.FileIndex / p.FileCount : 1);
                FileText = FileSizeConverter.Format(p.FileBytes) + " / " + FileSizeConverter.Format(p.FileTotal);
                TotalText = string.Format("{0} / {1} · {2}%", FileSizeConverter.Format(p.Bytes), FileSizeConverter.Format(p.TotalBytes), (int)Math.Round(TotalFraction * 100));
                var seconds = _clock.Elapsed.TotalSeconds;
                if (seconds > 0.5 && p.Bytes > 0)
                {
                    var speed = p.Bytes / seconds;
                    SpeedText = FileSizeConverter.Format((long)speed) + "/s";
                    var left = TimeSpan.FromSeconds((p.TotalBytes - p.Bytes) / speed);
                    RemainingText = left.TotalHours >= 1 ? left.ToString(@"h\:mm\:ss") : left.ToString(@"m\:ss");
                }
            }
            OnPropertyChanged(string.Empty);
        }
    }

    // Runs transfer jobs one after another ("Add to queue"), holding the shared busy lock while a job runs.
    public class JobQueue : ObservableObject
    {
        private readonly IStateMonitor _monitor;
        private readonly object _gate = new object();
        private bool _running;
        private TransferJob _current;

        public JobQueue(IStateMonitor monitor)
        {
            _monitor = monitor;
        }

        public ObservableCollection<TransferJob> Pending { get; } = new ObservableCollection<TransferJob>();

        public TransferJob Current
        {
            get => _current;
            private set
            {
                if (SetProperty(ref _current, value))
                    OnPropertyChanged(nameof(HasJob));
            }
        }

        public bool HasJob => _current != null;

        public Task Enqueue(TransferJob job)
        {
            lock (_gate)
            {
                Pending.Add(job);
                if (!_running)
                {
                    _running = true;
                    Task.Run(Worker);
                }
            }
            return job.Completion.Task;
        }

        private void Worker()
        {
            while (true)
            {
                TransferJob job;
                lock (_gate)
                {
                    if (Pending.Count == 0)
                    {
                        _running = false;
                        Dispatcher.UIThread.Post(() => Current = null);
                        return;
                    }
                    job = Pending[0];
                    Pending.RemoveAt(0);
                }
                // Zip, split and friends share the lock; wait for them instead of failing.
                bool entered;
                while (!(entered = _monitor.Enter()) && !job.Token.IsCancellationRequested)
                    Thread.Sleep(100);
                if (!entered)
                {
                    job.Skip();
                    continue;
                }
                Dispatcher.UIThread.Post(() => Current = job);
                try
                {
                    job.Run();
                }
                catch
                {
                    // Errors are logged by the job's work; the queue keeps going.
                }
                finally
                {
                    _monitor.Exit();
                }
            }
        }
    }
}
