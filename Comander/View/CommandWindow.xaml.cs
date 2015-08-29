using System.Windows;
using System.Windows.Controls;
using Comander.ViewModel;


namespace Comander.View
{

    public partial class CommandWindow : UserControl
    {

        public static readonly DependencyProperty ManagerProperty =
        DependencyProperty.Register("Manager", typeof(IOManager), typeof(CommandWindow), new UIPropertyMetadata(null));

        public IOManager Manager
        {
            get { return (IOManager)GetValue(ManagerProperty); }
            set
            {
                SetValue(ManagerProperty, value);
            }
        }

        static CommandWindow()
        {
            
        }

        public CommandWindow()
        {
            InitializeComponent();
        }
    }
}
