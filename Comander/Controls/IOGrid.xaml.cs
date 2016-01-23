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
                       int index = DataGrid1.SelectedIndex > 0 ? DataGrid1.SelectedIndex : 0;
                       DataGrid1.CurrentCell = new DataGridCellInfo(DataGrid1.Items[index], DataGrid1.Columns[0]);
                   }
               });
            }
        }

        private void DataGrid1_GotFocus(object sender, RoutedEventArgs e)
        {
            _messanger.Send(new FocusMessage
            {
                ManagerName = GridName
            });
        }
    }
}
