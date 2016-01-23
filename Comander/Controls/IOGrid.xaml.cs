using Comander.Messages;
using System.Windows;
using System.Windows.Controls;

namespace Comander.Controls
{
    public partial class IOGrid : UserControl
    {
        private Messanger.Messanger _messanger;

        public IOGrid()
        {
            InitializeComponent();
            _messanger = Messanger.Messanger.GetInstance();
            _messanger.Register(typeof(SetFocusMessage), SetFocusCallback);
        }

        public string GridName { get; set; }

        private void DataGrid1_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            _messanger.Send(new FocusMessage
            {
                ManagerType = GridName
            });
        }

        private void SetFocusCallback(object e)
        {
            var setFocusMessage = (SetFocusMessage)e;
            if(setFocusMessage.GridName.Equals(GridName, System.StringComparison.OrdinalIgnoreCase))
            {
                Application.Current.Dispatcher.Invoke(() =>
               {
                   if (DataGrid1.Items.Count > 0)
                   {
                       DataGrid1.Focus();
                       DataGrid1.CurrentItem = DataGrid1.Items[0];
                   }
               });
            }
        }
    }
}
