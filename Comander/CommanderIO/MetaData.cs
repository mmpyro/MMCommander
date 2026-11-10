using CommunityToolkit.Mvvm.ComponentModel;

namespace Comander.CommanderIO
{
    // Notifies so the grid's tick icon redraws in place; replacing Files would reset the cursor.
    public class MetaData : ObservableObject
    {
        private bool _isSelected;

        public MetaData() : this(null, false)
        {

        }

        public MetaData(string color, bool isSelected)
        {
            Color = color;
            IsSelected = isSelected;
        }

        public string Color { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
