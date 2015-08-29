using Comander.CommanderIO;
using Comander.View;

namespace Comander.ViewModel
{
    public class InfoVM : HidenVM
    {
        private IMetadataFileStructure _file;

        public InfoVM(HidenWindowBase window, IMetadataFileStructure file) : base(window)
        {
            _file = file;

        }

        public IMetadataFileStructure File
        {
            get { return _file; }
            set
            {
                if (Equals(value, _file)) return;
                _file = value;
                OnPropertyChanged("File");
            }
        }

    }
}