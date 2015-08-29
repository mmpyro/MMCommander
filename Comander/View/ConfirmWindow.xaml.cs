using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace Comander.View
{
    public enum WindowIcon
    {
        Info,
        Warrning,
        Error
    }
    
    public partial class ConfirmWindow : Window
    {

        public ConfirmWindow(string textMessage, WindowIcon icon = WindowIcon.Warrning)
        {
            InitializeComponent();
            this.InfoTextBlock.Text = textMessage;
            if (icon.Equals(WindowIcon.Warrning))
                this.Image.Source = new BitmapImage(new Uri(@"..\Icons\Alert-48.png", UriKind.Relative));
            else if(icon.Equals(WindowIcon.Error))
                this.Image.Source = new BitmapImage(new Uri(@"..\Icons\Cancel-48.png", UriKind.Relative));
            else
                this.Image.Source = new BitmapImage(new Uri(@"..\Icons\info_black.png", UriKind.Relative));
        }


        private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }


        private void Move_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
