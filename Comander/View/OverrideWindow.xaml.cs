using Comander.Dtos;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace Comander.View
{
    
    public partial class OverrideWindow : Window
    {
        private readonly CopyOptionDto copyOption;

        public OverrideWindow(string textMessage, CopyOptionDto copyOption, WindowIcon icon = WindowIcon.Warrning)
        {
            InitializeComponent();
            this.InfoTextBlock.Text = textMessage;
            if (icon.Equals(WindowIcon.Warrning))
                Image.Source = new BitmapImage(new Uri(@"..\Icons\Alert-48.png", UriKind.Relative));
            else if(icon.Equals(WindowIcon.Error))
                Image.Source = new BitmapImage(new Uri(@"..\Icons\Cancel-48.png", UriKind.Relative));
            else
                Image.Source = new BitmapImage(new Uri(@"..\Icons\info_black.png", UriKind.Relative));
            this.copyOption = copyOption;
        }


        private void OverrideSingleButtonClick(object sender, RoutedEventArgs e)
        {
            copyOption.Options = CopyOption.OverrideSingle;
            this.DialogResult = true;

        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            copyOption.Options = CopyOption.Cancel;
            this.DialogResult = false;
        }

        private void Move_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OverrideAllButtonClick(object sender, RoutedEventArgs e)
        {
            copyOption.Options = CopyOption.OverrideAll;
            this.DialogResult = true;
        }

        private void DotNotOverrideButtonClick(object sender, RoutedEventArgs e)
        {
            copyOption.Options = CopyOption.None;
            this.DialogResult = true;
        }
    }
}
