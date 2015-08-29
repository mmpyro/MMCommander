using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Comander.Controls
{
    internal class Circle
    {
        public Ellipse Ellipse { get; set; }
        public double Radius { get; set; }
        public int Size { get; set; }
        public double Angle { get; set; }
        private readonly double _center;

        public Circle(double angle, double center, double radius, double size, Brush brush)
        {
            Ellipse = new Ellipse();
            Ellipse.Fill = brush;
            Ellipse.Width = size;
            Ellipse.Height = size;
            Radius = radius;
            Angle = angle;
            _center = center;
        }

        private double Convert(double angle)
        {
            return (angle * Math.PI) / 180.0;
        }

        private double GetX()
        {
            return _center + (Radius * Math.Sin(Convert(Angle)));
        }

        private double GetY()
        {
            return _center + (Radius * Math.Cos(Convert(Angle)));
        }

        public void Move()
        {
            Canvas.SetLeft(Ellipse, GetX());
            Canvas.SetTop(Ellipse, GetY());
            Angle -= 20;
        }

    }

    public partial class Worker : UserControl
    {
        private readonly List<Circle> _circles = new List<Circle>();
        private int _numberOfElipses;
        private double _radius;
        private int _elipseSize;
        public string NumberOfEllipses
        {
            get
            {
                return _numberOfElipses.ToString();
            }
            set
            {
                _numberOfElipses = int.Parse(value);
            }
        }
        public string Radius
        {
            get
            {
                return _radius.ToString();
            }
            set
            {
                _radius = double.Parse(value);
            }
        }
        public string ElipseSize
        {
            get
            {
                return _elipseSize.ToString();
            }
            set
            {
                _elipseSize = int.Parse(value);
            }
        }
        public double Center
        {

            get
            {
                return Height / 2.0;
            }
        }

        private static bool IsEnable;
        private static readonly Object locker = new object();
        private Thread _th;


        public  Worker()
        {
            this.InitializeComponent();
        }

        public static void Wait()
        {
            lock (locker)
            {
                IsEnable = false;
            }
        }

        public static  void Pulse()
        {
            lock (locker)
            {
                IsEnable = true;
                Monitor.Pulse(locker);
            }
        }

        protected void Draw()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < _numberOfElipses; i++)
                {
                    _circles.Add(new Circle(20 + (i*20.0), Center, _radius, _elipseSize, Foreground));
                    _circles.Add(new Circle(20 + (i*20.0), Center, _radius - _elipseSize, _elipseSize, Foreground));
                }
            });
            while (true)
            {
                if (!IsEnable)
                    lock (locker)
                        Monitor.Wait(locker);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Circle circle in _circles)
                    {
                        this.DrawArea.Children.Add(circle.Ellipse);
                        circle.Move();
                    }
                });
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() => this.DrawArea.Children.Clear());
            }
        }

        public void Init()
        {
            _th = new Thread(Draw);
            _th.IsBackground = true;
            _th.Start();
        }

        /// <summary>
        /// Use during OnClose
        /// </summary>
        public  void DisposeWatcher()
        {
            _th.Abort();
        }
    }

}
