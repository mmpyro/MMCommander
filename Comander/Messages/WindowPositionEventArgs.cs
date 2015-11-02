using System.Windows;
using System.Windows.Input;

namespace Comander.Messages
{
    public class WindowPositionEventArgs
    {
        public Point CurrentCursorPosition { get; private set; }

        public WindowPositionEventArgs(Window sender,MouseEventArgs args)
        {
            var mousePosition = args.GetPosition(sender);
            CurrentCursorPosition = new Point(mousePosition.X+sender.Left, mousePosition.Y+sender.Top);
        }
    }
}
