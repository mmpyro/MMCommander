using System;
using System.Windows;
using System.Windows.Input;

namespace Comander
{
    public class MainWindowEventResolver
    {
        public Point MousePoint { get; set; }
        public Func<Point> GetWindowsPositionAction { get; set; }

        public void MainWindow_OnMouseMove(object sender, MouseEventArgs e)
        {
            MousePoint = e.GetPosition((Window)sender);
//            Debug.WriteLine(String.Format("[Point: [X: {0}, Y: {1}]]", MousePoint.X, MousePoint.Y));
        }

       
    }
}