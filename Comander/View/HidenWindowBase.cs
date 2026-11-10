using System;
using Avalonia.Input;
using Avalonia.Threading;

namespace Comander.View
{
    // Popup that closes 1.5 s after the pointer leaves it; re-entering cancels. A UI-thread timer replaces the old polling task (BUG-32).
    public class HidenWindowBase : DialogWindow
    {
        private readonly DispatcherTimer _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };

        public HidenWindowBase()
        {
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
            _hideTimer.Tick += (_, _) => Close();
            Closed += (_, _) => _hideTimer.Stop();
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            _hideTimer.Stop();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _hideTimer.Start();
        }
    }
}
