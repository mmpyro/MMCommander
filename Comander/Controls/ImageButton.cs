using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Comander.Controls
{
    public class ImageButton : Button
    {
        static ImageButton()
        {
        }

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageButton), new UIPropertyMetadata(null));

    }
}