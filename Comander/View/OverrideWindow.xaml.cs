using Comander.Dtos;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace Comander.View
{
    
    public partial class OverrideWindow : Window
    {
        private readonly CopyStatusDto copyStatus;

        public OverrideWindow(string textMessage, CopyStatusDto copyStatus, WindowIcon icon = WindowIcon.Warrning)
        {
            InitializeComponent();
            this.InfoTextBlock.Text = textMessage;
            if (icon.Equals(WindowIcon.Warrning))
                Image.Source = new BitmapImage(new Uri(@"..\Icons\Alert-48.png", UriKind.Relative));
            else if(icon.Equals(WindowIcon.Error))
                Image.Source = new BitmapImage(new Uri(@"..\Icons\Cancel-48.png", UriKind.Relative));
            else
                Image.Source = new BitmapImage(new Uri(@"..\Icons\info_black.png", UriKind.Relative));
            this.copyStatus = copyStatus;
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

        private void OverrideAllButtonClick(object sender, RoutedEventArgs e)
        {
            copyStatus.OverrideAll = true;
            this.DialogResult = true;
        }

        private void DotNotOverrideButtonClick(object sender, RoutedEventArgs e)
        {
            copyStatus.OverrideAny = true;
            this.DialogResult = true;
        }
    }
}
