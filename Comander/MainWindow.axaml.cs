using System;
using Avalonia.Controls;
using Comander.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Comander
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new WindowCloseEventArgs());
            base.OnClosed(e);
        }
    }
}
