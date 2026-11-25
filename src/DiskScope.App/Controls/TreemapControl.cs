using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DiskScope.App.Visuals;
using DiskScope.Core.Formatting;
using DiskScope.Core.Treemap;
using DiskScope.Scanning;

namespace DiskScope.App.Controls;

/// <summary>
/// Klasör boyutlarını iç içe dikdörtgenlerle gösteren treemap.
/// Üzerine gelince bilgi kartı, tıklayınca seçim, çift tıklayınca içeri girme olayı üretir.
/// </summary>
public sealed class TreemapControl : FrameworkElement
{
    private const int MaxDepth = 4;
    private const double Pad = 3;
    private const double HeaderHeight = 18;

    private static readonly FontFamily Font = new("Segoe UI Variable Text, Segoe UI");

    private static readonly Brush BackgroundBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x0C, 0x10, 0x1E)));
    private static readonly Brush ContainerBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x11, 0x16, 0x2B)));
    private static readonly Brush FolderBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x1A, 0x21, 0x40)));
    private static readonly Brush SmallFilesTileBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x20, 0x28, 0x4A)));
    private static readonly Brush HoverOverlay = Frozen(new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)));
    private static readonly Brush LabelBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA)));
    private static readonly Brush SubLabelBrush = Frozen(new SolidColorBrush(Color.FromArgb(0xC0, 0xF5, 0xF7, 0xFA)));
    private static readonly Brush HeaderLabelBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xA3, 0xAD, 0xCB)));

    private static readonly Pen ContainerPen = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0x22, 0x2A, 0x4A)), 1));
    private static readonly Pen HoverPen = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0x4C, 0x8D, 0xFF)), 1.5));
    private static readonly Pen SelectionPen = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF5)), 2));

    public static readonly DependencyProperty RevealProperty = DependencyProperty.Register(
        nameof(Reveal),
        typeof(double),
        typeof(TreemapControl),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Pen LeafPen = Frozen(new Pen(new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)), 1));

    private readonly List<Tile> _tiles = new();
    private readonly Popup _popup;
    private readonly TextBlock _popupTitle;
    private readonly TextBlock _popupDetail;
    private readonly TextBlock _popupPath;

    private ScanNode? _root;
    private bool _revealPending;
    private Tile? _hover;
    private ScanNode? _selected;

    public TreemapControl()
    {
        ClipToBounds = true;
        SnapsToDevicePixels = true;

        _popupTitle = new TextBlock
        {
            FontFamily = Font,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = LabelBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _popupDetail = new TextBlock
        {
            FontFamily = Font,
            FontSize = 12,
            Foreground = HeaderLabelBrush,
            Margin = new Thickness(0, 3, 0, 0)
        };

        _popupPath = new TextBlock
        {
            FontFamily = Font,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x62, 0x6C, 0x8E)),
            Margin = new Thickness(0, 3, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var panel = new StackPanel();
        panel.Children.Add(_popupTitle);
        panel.Children.Add(_popupDetail);
        panel.Children.Add(_popupPath);

        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x40)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x34, 0x3E, 0x6B)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            MaxWidth = 420,
            IsHitTestVisible = false,
            Child = panel
        };

        _popup = new Popup
        {
            Child = card,
            Placement = PlacementMode.Relative,
            PlacementTarget = this,
            AllowsTransparency = true,
            IsHitTestVisible = false
        };
    }

    /// <summary>Bir düğümün görünen adını üretir (örneğin "küçük dosyalar" için yerelleştirilmiş ad).</summary>
    public Func<ScanNode, string>? NameProvider { get; set; }

    /// <summary>Bilgi kartındaki ikinci satır (boyut, yüzde, dosya sayısı).</summary>
    public Func<ScanNode, string>? DetailProvider { get; set; }

    public Func<ScanNode, string>? PathProvider { get; set; }

    /// <summary>Dosya karolarının rengini belirler (örneğin uzantıya göre). Verilmezse kategori rengi kullanılır.</summary>
    public Func<ScanNode, Brush>? FileBrushProvider { get; set; }

    /// <summary>0 ile 1 arası açılış animasyonu değeri (karolar yumuşakça belirir).</summary>
    public double Reveal
    {
        get => (double)GetValue(RevealProperty);
        set => SetValue(RevealProperty, value);
    }

    public event EventHandler<ScanNode>? NodeClicked;

    public event EventHandler<ScanNode>? NodeActivated;

    public ScanNode? SelectedNode
    {
        get => _selected;
        set
        {
            _selected = value;
            InvalidateVisual();
        }
    }

    /// <summary>Fare şu anda hangi düğümün üzerinde (yoksa null).</summary>
    public ScanNode? HoverNode => _hover?.Node;

    public void SetRoot(ScanNode? root)
    {
        _root = root;
        _revealPending = root is not null;
        Rebuild();
    }

    /// <summary>Dil değişimi gibi durumlarda metinleri yeniden üretir.</summary>
    public void Refresh() => Rebuild();

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        Rebuild();
    }

    private void Rebuild()
    {
        _hover = null;
        _popup.IsOpen = false;
        _tiles.Clear();

        if (_root is not null && ActualWidth >= 8 && ActualHeight >= 8)
        {
            BuildLevel(_root, new Rect(0, 0, ActualWidth, ActualHeight), 0, _tiles);
        }

        if (_revealPending && _tiles.Count > 0)
        {
            _revealPending = false;

            BeginAnimation(RevealProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(420),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        InvalidateVisual();
    }

    private static void BuildLevel(ScanNode node, Rect area, int depth, List<Tile> output)
    {
        var children = node.Children;

        if (children.Count == 0 || area.Width <= 0 || area.Height <= 0)
        {
            return;
        }

        var sizes = new double[children.Count];

        for (var i = 0; i < sizes.Length; i++)
        {
            sizes[i] = children[i].Size;
        }

        var rects = SquarifiedTreemap.Layout(sizes, new TreemapRect(area.X, area.Y, area.Width, area.Height));

        for (var i = 0; i < children.Count; i++)
        {
            var r = rects[i];

            if (r.Width < 1.5 || r.Height < 1.5)
            {
                continue;
            }

            var child = children[i];
            var bounds = new Rect(r.X, r.Y, r.Width, r.Height);
            var tile = new Tile(child, bounds);

            output.Add(tile);

            var canExpand = child.IsDirectory
                            && child.Children.Count > 0
                            && depth < MaxDepth
                            && bounds.Width >= 60
                            && bounds.Height >= 46;

            if (!canExpand)
            {
                continue;
            }

            var hasHeader = bounds.Height >= 64 && bounds.Width >= 90;
            var top = hasHeader ? HeaderHeight : 0;

            var inner = new Rect(
                bounds.X + Pad,
                bounds.Y + Pad + top,
                bounds.Width - 2 * Pad,
                bounds.Height - 2 * Pad - top);

            if (inner.Width < 20 || inner.Height < 14)
            {
                continue;
            }

            tile.HasHeader = hasHeader;
            tile.Children = new List<Tile>();

            BuildLevel(child, inner, depth + 1, tile.Children);
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var full = new Rect(0, 0, ActualWidth, ActualHeight);

        dc.PushClip(new RectangleGeometry(full, 10, 10));
        dc.DrawRectangle(BackgroundBrush, null, full);

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var reveal = Math.Clamp(Reveal, 0, 1);
        var scale = 0.96 + 0.04 * reveal;

        dc.PushOpacity(reveal);
        dc.PushTransform(new ScaleTransform(scale, scale, ActualWidth / 2, ActualHeight / 2));

        foreach (var tile in _tiles)
        {
            DrawTile(dc, tile, dpi);
        }

        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private void DrawTile(DrawingContext dc, Tile tile, double dpi)
    {
        var b = tile.Bounds;
        var rect = new Rect(b.X + 0.5, b.Y + 0.5, Math.Max(0, b.Width - 1), Math.Max(0, b.Height - 1));

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var node = tile.Node;
        var isHover = ReferenceEquals(tile, _hover);

        if (tile.Children is not null)
        {
            dc.DrawRoundedRectangle(ContainerBrush, ContainerPen, rect, 5, 5);

            if (tile.HasHeader)
            {
                DrawHeader(dc, tile, rect, dpi);
            }

            foreach (var child in tile.Children)
            {
                DrawTile(dc, child, dpi);
            }

            if (isHover)
            {
                dc.DrawRoundedRectangle(null, HoverPen, rect, 5, 5);
            }
        }
        else
        {
            tile.Fill ??= node.IsDirectory
                ? FolderBrush
                : node.IsSmallFilesGroup
                    ? SmallFilesTileBrush
                    : FileBrushProvider?.Invoke(node) ?? CategoryPalette.GetTileBrush(node.Category);

            var isFile = !node.IsDirectory && !node.IsSmallFilesGroup;

            dc.DrawRoundedRectangle(tile.Fill, isFile ? LeafPen : null, rect, 3, 3);
            DrawLabel(dc, tile, rect, dpi);

            if (isHover)
            {
                dc.DrawRoundedRectangle(HoverOverlay, HoverPen, rect, 3, 3);
            }
        }

        if (_selected is not null && ReferenceEquals(node, _selected))
        {
            dc.DrawRoundedRectangle(null, SelectionPen, rect, 3, 3);
        }
    }

    private void DrawHeader(DrawingContext dc, Tile tile, Rect rect, double dpi)
    {
        tile.NameText ??= MakeText(
            $"{DisplayName(tile.Node)}  ·  {SizeFormatter.Format(tile.Node.Size)}",
            11,
            FontWeights.SemiBold,
            HeaderLabelBrush,
            rect.Width - 12,
            HeaderHeight,
            dpi);

        dc.DrawText(tile.NameText, new Point(rect.X + 7, rect.Y + 3));
    }

    private void DrawLabel(DrawingContext dc, Tile tile, Rect rect, double dpi)
    {
        if (rect.Width < 64 || rect.Height < 28)
        {
            return;
        }

        tile.NameText ??= MakeText(
            DisplayName(tile.Node),
            12,
            FontWeights.SemiBold,
            LabelBrush,
            rect.Width - 14,
            rect.Height - 8,
            dpi);

        dc.DrawText(tile.NameText, new Point(rect.X + 7, rect.Y + 5));

        if (rect.Height >= 46)
        {
            tile.SizeText ??= MakeText(
                SizeFormatter.Format(tile.Node.Size),
                11,
                FontWeights.Normal,
                SubLabelBrush,
                rect.Width - 14,
                16,
                dpi);

            dc.DrawText(tile.SizeText, new Point(rect.X + 7, rect.Y + 5 + tile.NameText.Height + 1));
        }
    }

    private string DisplayName(ScanNode node) => NameProvider?.Invoke(node) ?? node.Name;

    private static FormattedText MakeText(
        string text,
        double size,
        FontWeight weight,
        Brush brush,
        double maxWidth,
        double maxHeight,
        double dpi) =>
        new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(Font, FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            dpi)
        {
            MaxTextWidth = Math.Max(1, maxWidth),
            MaxTextHeight = Math.Max(1, maxHeight),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis
        };

    private static Tile? FindTile(IEnumerable<Tile> tiles, Point point)
    {
        foreach (var tile in tiles)
        {
            if (!tile.Bounds.Contains(point))
            {
                continue;
            }

            if (tile.Children is not null)
            {
                return FindTile(tile.Children, point) ?? tile;
            }

            return tile;
        }

        return null;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hit = FindTile(_tiles, point);

        if (!ReferenceEquals(hit, _hover))
        {
            _hover = hit;
            UpdatePopupContent();
            InvalidateVisual();
        }

        if (hit is null)
        {
            _popup.IsOpen = false;
            return;
        }

        _popup.HorizontalOffset = point.X + 16;
        _popup.VerticalOffset = point.Y + 18;

        if (!_popup.IsOpen)
        {
            _popup.IsOpen = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        _hover = null;
        _popup.IsOpen = false;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        var hit = FindTile(_tiles, e.GetPosition(this));

        if (hit is null)
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            NodeActivated?.Invoke(this, hit.Node);
            return;
        }

        SelectedNode = hit.Node;
        NodeClicked?.Invoke(this, hit.Node);
    }

    private void UpdatePopupContent()
    {
        if (_hover is null)
        {
            return;
        }

        var node = _hover.Node;

        _popupTitle.Text = DisplayName(node);
        _popupDetail.Text = DetailProvider?.Invoke(node) ?? SizeFormatter.Format(node.Size);
        _popupPath.Text = PathProvider?.Invoke(node) ?? string.Empty;
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }

    private sealed class Tile
    {
        public Tile(ScanNode node, Rect bounds)
        {
            Node = node;
            Bounds = bounds;
        }

        public ScanNode Node { get; }

        public Rect Bounds { get; }

        public List<Tile>? Children { get; set; }

        public bool HasHeader { get; set; }

        public Brush? Fill { get; set; }

        public FormattedText? NameText { get; set; }

        public FormattedText? SizeText { get; set; }
    }
}
