using Comander.CommanderIO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Comander.ViewModel
{
    public class InfoVM : ObservableObject
    {
        private IMetadataFileStructure _file;

        public InfoVM(IMetadataFileStructure file)
        {
            _file = file;
        }

        public IMetadataFileStructure File
        {
            get { return _file; }
            set { SetProperty(ref _file, value); }
        }
    }
}
