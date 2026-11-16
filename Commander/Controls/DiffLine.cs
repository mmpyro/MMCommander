using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Commander.Controls
{
    // One side of a compare-files row: the line text with the differing characters on SpanBrush,
    // shifted left by the inherited Offset so both sides scroll horizontally together.
    public class DiffLine : TextBlock
    {
        public static readonly StyledProperty<string> LineProperty = AvaloniaProperty.Register<DiffLine, string>(nameof(Line));

        public static readonly StyledProperty<IReadOnlyList<(int Start, int Length)>> SpansProperty =
            AvaloniaProperty.Register<DiffLine, IReadOnlyList<(int Start, int Length)>>(nameof(Spans));

        public static readonly StyledProperty<IBrush> SpanBrushProperty = AvaloniaProperty.Register<DiffLine, IBrush>(nameof(SpanBrush));

        // Set once on the list; every row inherits it.
        public static readonly AttachedProperty<double> OffsetProperty =
            AvaloniaProperty.RegisterAttached<DiffLine, Control, double>("Offset", inherits: true);

        // Lives in a Canvas, so it is laid out at full length; the cell around the Canvas clips.
        public DiffLine()
        {
            TextWrapping = TextWrapping.NoWrap;
        }

        // Keeps the global TextBlock and .mono styles.
        protected override Type StyleKeyOverride => typeof(TextBlock);

        public string Line
        {
            get => GetValue(LineProperty);
            set => SetValue(LineProperty, value);
        }

        public IReadOnlyList<(int Start, int Length)> Spans
        {
            get => GetValue(SpansProperty);
            set => SetValue(SpansProperty, value);
        }

        public IBrush SpanBrush
        {
            get => GetValue(SpanBrushProperty);
            set => SetValue(SpanBrushProperty, value);
        }

        public static double GetOffset(Control control) => control.GetValue(OffsetProperty);
        public static void SetOffset(Control control, double value) => control.SetValue(OffsetProperty, value);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == LineProperty || change.Property == SpansProperty || change.Property == SpanBrushProperty)
                Build();
            else if (change.Property == OffsetProperty)
                RenderTransform = new TranslateTransform(-GetOffset(this), 0);
        }

        private void Build()
        {
            var line = Line ?? string.Empty;
            var spans = Spans;
            if (spans == null || spans.Count == 0)
            {
                Inlines = null;
                Text = line;
                return;
            }
            var inlines = new InlineCollection();
            int at = 0;
            foreach (var (start, length) in spans)
            {
                if (start >= line.Length) break;
                if (start > at) inlines.Add(new Run(line.Substring(at, start - at)));
                int end = Math.Min(start + length, line.Length);
                inlines.Add(new Run(line.Substring(start, end - start)) { Background = SpanBrush });
                at = end;
            }
            if (at < line.Length) inlines.Add(new Run(line.Substring(at)));
            Inlines = inlines;
        }

    }
}
