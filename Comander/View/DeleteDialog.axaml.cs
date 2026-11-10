using Comander.Dtos;

namespace Comander.View
{
    // ShowDialog<DeleteRequest>: null when cancelled. Permanent mode turns the card red and warns.
    public partial class DeleteDialog : DialogWindow
    {
        private readonly DeleteDialogModel _model;

        public DeleteDialog(DeleteDialogModel model)
        {
            InitializeComponent();
            _model = model;
            ItemList.ItemsSource = model.Items;
            SubtitleText.Text = "From " + model.Folder + " · " + model.TotalSizeText;
            TrashItem.IsEnabled = model.TrashUnavailable == null;
            ModeList.SelectedIndex = model.Permanent || model.TrashUnavailable != null ? 1 : 0;
            ModeList.SelectionChanged += (_, _) => Update();
            CancelButton.Click += (_, _) => Close(null);
            OkButton.Click += (_, _) => Close(new DeleteRequest { Permanent = Permanent, DontAskAgain = DontAskAgain });
            Opened += (_, _) => OkButton.Focus();
            Update();
        }

        public bool Permanent => ModeList.SelectedIndex == 1;

        public bool DontAskAgain => !Permanent && DontAskBox.IsChecked == true;

        private void Update()
        {
            var n = _model.Items.Count;
            var items = n + " item" + (n == 1 ? string.Empty : "s");
            TitleText.Text = Permanent ? "Permanently delete " + items + "?" : "Move " + items + " to Trash?";
            OkText.Text = (Permanent ? "Delete permanently" : "Move to Trash") + "  ↵";
            Classes.Set("danger", Permanent);
            ModeList.Classes.Set("danger", Permanent);
            OkButton.Classes.Set("accent", !Permanent);
            OkButton.Classes.Set("dangerfill", Permanent);
            DontAskBox.IsVisible = !Permanent;
            HintText.Classes.Set("error", Permanent);
            HintText.Classes.Set("muted", !Permanent);
            HintText.Text = Permanent
                ? (_model.TrashUnavailable != null ? "Trash can't be used: " + _model.TrashUnavailable + ". " : string.Empty)
                  + "These files will be removed immediately and can't be recovered."
                : "You can restore these from the Trash. Press Shift+F8 to skip the Trash.";
        }
    }
}
