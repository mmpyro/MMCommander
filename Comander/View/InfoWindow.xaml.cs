using Comander.CommanderIO;
using Comander.ViewModel;

namespace Comander.View
{
    public partial class InfoWindow : HidenWindowBase
    {
        public InfoWindow(IMetadataFileStructure fileInfo)
        {
            InitializeComponent();
            InitTask();
            var infoVm = new InfoVM(this, fileInfo);
            DataContext = infoVm;  
        }
    }
}
