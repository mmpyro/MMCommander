namespace Comander.CommanderIO
{
    public class MetaData
    {
        public MetaData() : this(null, false)
        {

        }

        public MetaData(string color, bool isSelected)
        {
            Color = color;
            IsSelected = isSelected;
        }

        public string Color { get; set; }
        public bool IsSelected { get; set; }
    }
}