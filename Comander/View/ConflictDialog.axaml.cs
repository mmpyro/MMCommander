using System;
using System.IO;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Comander.Converters;
using IOLib;

namespace Comander.View
{
    // ShowDialog<ConflictAnswer>. Esc skips only this file, so nothing is overwritten by accident.
    public partial class ConflictDialog : DialogWindow
    {
        private readonly Conflict _conflict;

        public ConflictDialog(Conflict conflict)
        {
            InitializeComponent();
            _conflict = conflict;
            TitleText.Text = Path.GetFileName(conflict.TargetPath) + " already exists";
            SubtitleText.Text = "in " + Path.GetDirectoryName(conflict.TargetPath) + " · conflict " + conflict.Index;
            ExistingText.Text = Describe(conflict.TargetSize, conflict.TargetTime);
            IncomingText.Text = Describe(conflict.SourceSize, conflict.SourceTime) + (conflict.SourceTime > conflict.TargetTime ? " · newer" : string.Empty);
            Thumbnail(conflict.TargetPath, ExistingImage, ExistingIcon);
            Thumbnail(conflict.SourcePath, IncomingImage, IncomingIcon);
            SkipButton.Click += (_, _) => Answer(ConflictDecision.Skip);
            KeepBothButton.Click += (_, _) => Answer(ConflictDecision.KeepBoth);
            ReplaceButton.Click += (_, _) => Answer(ConflictDecision.Replace);
            CompareButton.Click += (_, _) => Compare();
            Opened += (_, _) => ReplaceButton.Focus();
        }

        private void Answer(ConflictDecision decision) => Close(new ConflictAnswer(decision, AllBox.IsChecked == true));

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Answer(ConflictDecision.Skip);
                return;
            }
            base.OnKeyDown(e);
        }

        private void Compare()
        {
            try
            {
                CompareText.Text = TransferEngine.SameHash(_conflict.SourcePath, _conflict.TargetPath) ? "Identical content" : "Different content";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                CompareText.Text = "Cannot compare: " + ex.Message;
            }
        }

        private static string Describe(long size, DateTime time) =>
            FileSizeConverter.Format(size) + " · " + new DateConverter().Convert(time, typeof(string), null, null);

        private static readonly string[] ImageExts = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

        private static void Thumbnail(string path, Avalonia.Controls.Image image, Avalonia.Controls.Shapes.Path icon)
        {
            if (Array.IndexOf(ImageExts, Path.GetExtension(path).ToLowerInvariant()) >= 0)
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    image.Source = Bitmap.DecodeToHeight(stream, 96);
                    icon.IsVisible = false;
                    return;
                }
                catch (Exception)
                {
                    // Not a readable image; fall back to the icon.
                }
            }
            image.IsVisible = false;
        }
    }
}
