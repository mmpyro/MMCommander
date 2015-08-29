namespace Comander.Other
{
    public class FileMetadata
    {
        private bool _isSelected = false;
        public string Color { get; set; }

        public void SelectFile()
        {
            _isSelected = !_isSelected;
        }

        public bool IsSelected()
        {
            return _isSelected;
        }
    }
}