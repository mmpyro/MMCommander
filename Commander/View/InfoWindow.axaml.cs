using Commander.CommanderIO;
using Commander.ViewModel;

namespace Commander.View
{
    public partial class InfoWindow : HidenWindowBase
    {
        public InfoWindow(IMetadataFileStructure fileInfo)
        {
            InitializeComponent();
            DataContext = new InfoVM(fileInfo);
        }
    }
}
