using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DiskScope.App.Controls;

/// <summary>Doluluk yüzdesini halka olarak gösterir; değer değişince halka yumuşakça dolar.</summary>
public sealed class UsageRing : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(UsageRing),
        new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress),
        typeof(double),
        typeof(UsageRing),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush),
        typeof(Brush),
        typeof(UsageRing),
        new FrameworkPropertyMetadata(Brushes.Teal, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush),
        typeof(Brush),
        typeof(UsageRing),
        new FrameworkPropertyMetadata(Brushes.DimGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness),
        typeof(double),
        typeof(UsageRing),
        new FrameworkPropertyMetadata(9.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Hedef yüzde (0-100).</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Ekranda şu an gösterilen (animasyonlu) yüzde.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Brush RingBrush
    {
        get => (Brush)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ring = (UsageRing)d;
        var target = Math.Clamp((double)e.NewValue, 0, 100);

        ring.BeginAnimation(ProgressProperty, new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(900),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var thickness = Thickness;
        var size = Math.Min(ActualWidth, ActualHeight);

        if (size <= thickness * 2)
        {
            return;
        }

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = (size - thickness) / 2;

        dc.DrawEllipse(null, new Pen(TrackBrush, thickness), center, radius, radius);

        var percent = Math.Clamp(Progress, 0, 100);

        if (percent <= 0.1)
        {
            return;
        }

        var pen = new Pen(RingBrush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        if (percent >= 99.9)
        {
            dc.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var startAngle = -Math.PI / 2;
        var endAngle = startAngle + 2 * Math.PI * percent / 100;

        var start = new Point(center.X + radius * Math.Cos(startAngle), center.Y + radius * Math.Sin(startAngle));
        var end = new Point(center.X + radius * Math.Cos(endAngle), center.Y + radius * Math.Sin(endAngle));

        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, percent > 50, SweepDirection.Clockwise, true, false);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
