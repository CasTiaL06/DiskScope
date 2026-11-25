using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DiskScope.App.Controls;

public sealed record ChartPoint(string Label, double Value);

/// <summary>
/// Günlük doluluk yüzdesini alan grafik olarak çizer (0-100%).
/// İki noktadan az veri varsa grafik yerine açıklayıcı bir mesaj gösterir; veri uydurmaz.
/// </summary>
public sealed class UsageChart : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points),
        typeof(IReadOnlyList<ChartPoint>),
        typeof(UsageChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    public static readonly DependencyProperty EmptyMessageProperty = DependencyProperty.Register(
        nameof(EmptyMessage),
        typeof(string),
        typeof(UsageChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly DependencyProperty RevealProperty = DependencyProperty.Register(
        nameof(Reveal),
        typeof(double),
        typeof(UsageChart),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly FontFamily Font = new("Segoe UI Variable Text, Segoe UI");

    private static readonly Brush LabelBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xA3, 0xAD, 0xCB)));
    private static readonly Pen GridPen = Frozen(new Pen(new SolidColorBrush(Color.FromArgb(0x40, 0x7C, 0x8B, 0xB5)), 1));

    public IReadOnlyList<ChartPoint>? Points
    {
        get => (IReadOnlyList<ChartPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    private double Reveal
    {
        get => (double)GetValue(RevealProperty);
        set => SetValue(RevealProperty, value);
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Çizgi soldan sağa yumuşakça çizilir.
        ((UsageChart)d).BeginAnimation(RevealProperty, new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(900),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var points = Points;

        const double left = 44;
        const double right = 16;
        const double top = 12;
        const double bottom = 28;

        var plot = new Rect(left, top, Math.Max(0, ActualWidth - left - right), Math.Max(0, ActualHeight - top - bottom));

        if (plot.Width < 40 || plot.Height < 40)
        {
            return;
        }

        // Yatay kılavuz çizgileri ve yüzde etiketleri
        foreach (var percent in new[] { 0, 25, 50, 75, 100 })
        {
            var y = plot.Top + (1 - percent / 100.0) * plot.Height;

            dc.DrawLine(GridPen, new Point(plot.Left, y), new Point(plot.Right, y));

            var label = Text($"{percent}%", 11, LabelBrush, dpi);
            dc.DrawText(label, new Point(plot.Left - 8 - label.Width, y - label.Height / 2));
        }

        if (points is null || points.Count < 2)
        {
            var message = Text(EmptyMessage, 12, LabelBrush, dpi);
            message.MaxTextWidth = Math.Max(40, plot.Width - 40);
            message.TextAlignment = TextAlignment.Center;

            dc.DrawText(message, new Point(plot.Left + 20, plot.Top + (plot.Height - message.Height) / 2));
            return;
        }

        var step = plot.Width / (points.Count - 1);
        var positions = new Point[points.Count];

        for (var i = 0; i < points.Count; i++)
        {
            var value = Math.Clamp(points[i].Value, 0, 100);

            positions[i] = new Point(plot.Left + i * step, plot.Top + (1 - value / 100.0) * plot.Height);

            var day = Text(points[i].Label, 11, LabelBrush, dpi);
            dc.DrawText(day, new Point(positions[i].X - day.Width / 2, plot.Bottom + 8));
        }

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, Math.Max(0, ActualWidth * Math.Clamp(Reveal, 0, 1)), ActualHeight)));

        // Alan dolgusu
        var area = new StreamGeometry();

        using (var context = area.Open())
        {
            context.BeginFigure(new Point(positions[0].X, plot.Bottom), true, true);

            foreach (var position in positions)
            {
                context.LineTo(position, true, false);
            }

            context.LineTo(new Point(positions[^1].X, plot.Bottom), true, false);
        }

        area.Freeze();

        var fill = new LinearGradientBrush(
            Color.FromArgb(0x70, 0x4C, 0x8D, 0xFF),
            Color.FromArgb(0x00, 0x8B, 0x5C, 0xF6),
            90);

        dc.DrawGeometry(fill, null, area);

        // Çizgi
        var line = new StreamGeometry();

        using (var context = line.Open())
        {
            context.BeginFigure(positions[0], false, false);

            for (var i = 1; i < positions.Length; i++)
            {
                context.LineTo(positions[i], true, true);
            }
        }

        line.Freeze();

        var stroke = new LinearGradientBrush(Color.FromRgb(0x4C, 0x8D, 0xFF), Color.FromRgb(0x8B, 0x5C, 0xF6), 0);
        var pen = new Pen(stroke, 3) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        dc.DrawGeometry(null, pen, line);

        // Noktalar
        var dotFill = new SolidColorBrush(Color.FromRgb(0x0A, 0x0D, 0x17));
        var dotPen = new Pen(new SolidColorBrush(Color.FromRgb(0x7F, 0xB0, 0xFF)), 2);

        foreach (var position in positions)
        {
            dc.DrawEllipse(dotFill, dotPen, position, 4.5, 4.5);
        }

        dc.Pop();

        // Son değerin balonu
        if (Reveal >= 0.98)
        {
            var last = positions[^1];
            var bubble = Text(string.Format(CultureInfo.CurrentCulture, "{0:0.0}%", points[^1].Value), 12, Brushes.White, dpi);

            var box = new Rect(
                Math.Min(last.X - bubble.Width / 2 - 8, ActualWidth - bubble.Width - 18),
                Math.Max(0, last.Y - bubble.Height - 18),
                bubble.Width + 16,
                bubble.Height + 8);

            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x40)),
                new Pen(new SolidColorBrush(Color.FromRgb(0x34, 0x3E, 0x6B)), 1),
                box,
                6,
                6);

            dc.DrawText(bubble, new Point(box.X + 8, box.Y + 4));
        }
    }

    private static FormattedText Text(string text, double size, Brush brush, double dpi) =>
        new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            size,
            brush,
            dpi);

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
