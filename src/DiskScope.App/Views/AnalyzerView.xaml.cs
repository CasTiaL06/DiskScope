using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DiskScope.App.Helpers;
using DiskScope.App.Localization;
using DiskScope.App.Services;
using DiskScope.App.Visuals;
using DiskScope.Core.Formatting;
using DiskScope.Scanning;
using Microsoft.Win32;
using Serilog;

namespace DiskScope.App.Views;

public partial class AnalyzerView : UserControl
{
    private enum ViewState
    {
        Empty,
        Scanning,
        Result
    }

    private readonly FolderScanner _scanner = new();

    private CancellationTokenSource? _cts;
    private ScanResult? _result;
    private ScanNode? _current;
    private ScanNode? _contextNode;
    private string? _scanPath;
    private long? _expectedBytes;
    private ScanProgress? _lastProgress;
    private bool _isScanning;
    private bool _syncingSelection;
    private Func<string> _emptyBody = () => Loc.T("EmptyBody");

    public AnalyzerView()
    {
        InitializeComponent();

        Treemap.NameProvider = NodeName;
        Treemap.DetailProvider = NodeDetail;
        Treemap.PathProvider = node => node.GetFullPath();
        Treemap.FileBrushProvider = node => ExtensionPalette.GetTileBrush(Path.GetExtension(node.Name), node.Category);
        Treemap.NodeClicked += Treemap_NodeClicked;
        Treemap.NodeActivated += Treemap_NodeActivated;

        LocalizationManager.Instance.PropertyChanged += (_, _) => RefreshTexts();

        SetState(ViewState.Empty);
        RefreshTexts();
    }

    /// <summary>"Disk seç" düğmesine basıldı (Genel Bakış'a geçilmesi istenir).</summary>
    public event EventHandler? ChooseDiskRequested;

    public bool IsScanning => _isScanning;

    // ------------------------------------------------------------------
    // Tarama
    // ------------------------------------------------------------------

    public async Task StartScanAsync(string path, long? expectedBytes)
    {
        if (_isScanning)
        {
            return;
        }

        _scanPath = path;
        _expectedBytes = expectedBytes;
        _lastProgress = null;
        _result = null;
        _current = null;
        _isScanning = true;
        _cts = new CancellationTokenSource();

        SetState(ViewState.Scanning);
        UpdateScanUi();
        UpdateButtons();

        var progress = new Progress<ScanProgress>(p =>
        {
            if (!_isScanning)
            {
                return;
            }

            _lastProgress = p;
            UpdateScanUi();
        });

        try
        {
            var result = await _scanner.ScanAsync(path, null, progress, _cts.Token);

            _isScanning = false;
            _result = result;

            _ = SaveSummaryAsync(result);

            SetState(ViewState.Result);
            BuildPanels();
            ShowFolder(result.Root);
        }
        catch (Exception ex) when (IsCancellation(ex))
        {
            _isScanning = false;
            _emptyBody = () => Loc.T("ScanCancelledBody");
            SetState(ViewState.Empty);
        }
        catch (Exception ex)
        {
            _isScanning = false;
            Log.Error(ex, "Tarama başarısız oldu: {Path}", path);

            var message = ex.Message;
            _emptyBody = () => Loc.F("ScanFailedBody", message);
            SetState(ViewState.Empty);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _isScanning = false;

            UpdateScanUi();
            UpdateButtons();
        }
    }

    /// <summary>Tamamlanan taramanın özetini geçmişe kaydeder (Genel Bakış ve Tarama Geçmişi için).</summary>
    private static async Task SaveSummaryAsync(ScanResult result)
    {
        try
        {
            await AppServices.AddScanAsync(ScanSummaryFactory.Create(result, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Tarama özeti kaydedilemedi.");
        }
    }

    private static bool IsCancellation(Exception ex) =>
        ex is OperationCanceledException
        || (ex is AggregateException aggregate
            && aggregate.InnerExceptions.All(inner => inner is OperationCanceledException));

    // ------------------------------------------------------------------
    // Görünüm durumu
    // ------------------------------------------------------------------

    private void SetState(ViewState state)
    {
        EmptyPanel.Visibility = state == ViewState.Empty ? Visibility.Visible : Visibility.Collapsed;
        ScanningPanel.Visibility = state == ViewState.Scanning ? Visibility.Visible : Visibility.Collapsed;
        ResultGrid.Visibility = state == ViewState.Result ? Visibility.Visible : Visibility.Collapsed;
        StatusPanel.Visibility = state == ViewState.Empty ? Visibility.Collapsed : Visibility.Visible;

        if (state == ViewState.Empty)
        {
            EmptyBodyText.Text = _emptyBody();
            BreadcrumbList.ItemsSource = null;
        }

        UpdateSpinner(state == ViewState.Scanning);
        UpdateButtons();
    }

    private void UpdateSpinner(bool spinning)
    {
        if (!spinning)
        {
            SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            return;
        }

        SpinnerRotate.BeginAnimation(
            RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever });
    }

    private void UpdateButtons()
    {
        RescanButton.IsEnabled = !_isScanning && _scanPath is not null;
        PickFolderButton.IsEnabled = !_isScanning;
        UpButton.IsEnabled = !_isScanning && _current?.Parent is not null;
        CancelButton.Visibility = _isScanning ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.IsEnabled = _isScanning;
    }

    private void UpdateScanUi()
    {
        if (_isScanning)
        {
            var progress = _lastProgress;
            var percent = GetPercent(progress);

            StatusText.Text = Loc.F("StatusScanning", progress?.CurrentPath ?? _scanPath ?? string.Empty);

            // Yüzde bilinmiyorsa (klasör taraması) çubuk gizlenir; sayaçlar ilerlemeyi gösterir.
            ScanProgressBar.Visibility = percent is null ? Visibility.Collapsed : Visibility.Visible;
            ScanProgressBar.Value = percent ?? 0;

            ScanPercentText.Text = percent is null ? "…" : $"{percent:0}%";
            ScanPathText.Text = progress?.CurrentPath ?? _scanPath ?? string.Empty;

            SetStats(
                progress?.Elapsed ?? TimeSpan.Zero,
                progress?.Files ?? 0,
                progress?.Folders ?? 0,
                progress?.Bytes ?? 0);

            SkippedText.Visibility = Visibility.Collapsed;
            return;
        }

        ScanProgressBar.Visibility = Visibility.Collapsed;

        if (_result is not null)
        {
            StatusText.Text = Loc.T("StatusDone");

            SetStats(_result.Elapsed, _result.TotalFiles, _result.TotalFolders, _result.TotalSize);

            SkippedText.Text = Loc.F("StatSkipped", _result.IssueCount);
            SkippedText.Visibility = _result.IssueCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private double? GetPercent(ScanProgress? progress)
    {
        if (progress is null || _expectedBytes is not > 0)
        {
            return null;
        }

        // Okunan boyut ile diskteki kullanılan alan birebir aynı olmaz; %99'da bekletiriz.
        return Math.Min(99, progress.Bytes * 100.0 / _expectedBytes.Value);
    }

    private void SetStats(TimeSpan elapsed, long files, long folders, long bytes)
    {
        ElapsedText.Text = Loc.F("StatElapsed", FormatElapsed(elapsed));
        FilesText.Text = Loc.F("StatFiles", files);
        FoldersText.Text = Loc.F("StatFolders", folders);
        SizeText.Text = Loc.F("StatSize", SizeFormatter.Format(bytes));
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalHours >= 1 ? elapsed.ToString(@"h\:mm\:ss") : elapsed.ToString(@"mm\:ss");

    /// <summary>Dil değişince kod ile yazılan metinleri yeniler.</summary>
    private void RefreshTexts()
    {
        EmptyBodyText.Text = _emptyBody();

        UpdateScanUi();

        if (_result is null)
        {
            return;
        }

        BuildPanels();
        Treemap.Refresh();

        if (_current is not null)
        {
            ShowFolder(_current);
        }
    }

    // ------------------------------------------------------------------
    // Sonuç ekranı
    // ------------------------------------------------------------------

    private void ShowFolder(ScanNode node)
    {
        _current = node;

        var rows = new List<FolderRow>(node.Children.Count);

        foreach (var child in node.Children)
        {
            var percent = node.Size > 0 ? child.Size * 100.0 / node.Size : 0;

            rows.Add(new FolderRow(child, NodeName(child), SizeFormatter.Format(child.Size), percent));
        }

        _syncingSelection = true;
        FolderList.ItemsSource = rows;
        _syncingSelection = false;

        Treemap.SelectedNode = null;
        Treemap.SetRoot(node);

        ListInfoText.Text = Loc.F("ListInfo", node.Children.Count, SizeFormatter.Format(node.Size));

        BuildBreadcrumb(node);
        UpdateButtons();
    }

    private void BuildBreadcrumb(ScanNode node)
    {
        var chain = new List<ScanNode>();

        for (var n = node; n is not null; n = n.Parent)
        {
            chain.Insert(0, n);
        }

        var items = new List<BreadcrumbItem>(chain.Count);

        for (var i = 0; i < chain.Count; i++)
        {
            items.Add(new BreadcrumbItem(chain[i], chain[i].Name, isLast: i == chain.Count - 1));
        }

        BreadcrumbList.ItemsSource = items;
    }

    private void BuildPanels()
    {
        if (_result is null)
        {
            return;
        }

        var total = _result.TotalSize;

        // Uzantı renkleri önce dağıtılır; treemap ve sağ panel aynı renkleri kullanır.
        ExtensionPalette.Assign(_result.Extensions);

        SumSizeValue.Text = SizeFormatter.Format(total);
        SumFilesValue.Text = _result.TotalFiles.ToString("N0");
        SumFoldersValue.Text = _result.TotalFolders.ToString("N0");
        SumTimeValue.Text = FormatElapsed(_result.Elapsed);

        // Kategori çubuğu ve listesi
        TypeBar.Children.Clear();
        TypeBar.ColumnDefinitions.Clear();

        var typeRows = new List<StatRow>();

        foreach (var category in _result.Categories.Where(c => c.Size > 0))
        {
            var brush = CategoryPalette.GetBrush(category.Category);
            var name = CategoryPalette.GetName(category.Category);

            TypeBar.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(category.Size, GridUnitType.Star)
            });

            var segment = new Border
            {
                Background = brush,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 1, 0),
                ToolTip = name
            };

            Grid.SetColumn(segment, TypeBar.ColumnDefinitions.Count - 1);
            TypeBar.Children.Add(segment);

            typeRows.Add(new StatRow(brush, name, SizeFormatter.Format(category.Size), Percent(category.Size, total)));
        }

        TypeList.ItemsSource = typeRows;

        // En büyük uzantılar
        var extRows = _result.Extensions
            .Where(e => e.Size > 0)
            .Take(8)
            .Select(e => new StatRow(
                ExtensionPalette.GetBrush(e.Extension, e.Category),
                e.Extension.Length == 0 ? Loc.T("NoExtension") : e.Extension,
                SizeFormatter.Format(e.Size),
                Percent(e.Size, total)))
            .ToList();

        ExtList.ItemsSource = extRows;
    }

    private static string Percent(long part, long total) =>
        total > 0 ? $"{part * 100.0 / total:0.0}%" : "0%";

    private string NodeName(ScanNode node) =>
        node.IsSmallFilesGroup ? Loc.F("SmallFilesGroupFormat", node.FileCount) : node.Name;

    private string NodeDetail(ScanNode node)
    {
        var total = _result?.TotalSize ?? 0;
        var percent = total > 0 ? node.Size * 100.0 / total : 0;
        var size = SizeFormatter.Format(node.Size);

        if (node.IsDirectory || node.IsSmallFilesGroup)
        {
            return Loc.F("DetailFolder", size, percent, node.FileCount);
        }

        return Loc.F("DetailFile", size, percent, CategoryPalette.GetName(node.Category));
    }

    // ------------------------------------------------------------------
    // Seçim ve gezinti
    // ------------------------------------------------------------------

    private void SelectNode(ScanNode node)
    {
        // Treemap'te iç içe bir düğüm seçilmiş olabilir; listede bu düğümün "mevcut klasörün çocuğu" olan atasını seç.
        var top = node;

        while (top.Parent is not null && !ReferenceEquals(top.Parent, _current))
        {
            top = top.Parent;
        }

        if (!ReferenceEquals(top.Parent, _current) || FolderList.ItemsSource is not List<FolderRow> rows)
        {
            return;
        }

        var row = rows.FirstOrDefault(r => ReferenceEquals(r.Node, top));

        if (row is null)
        {
            return;
        }

        _syncingSelection = true;
        FolderList.SelectedItem = row;
        FolderList.ScrollIntoView(row);
        _syncingSelection = false;
    }

    private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection)
        {
            return;
        }

        Treemap.SelectedNode = (FolderList.SelectedItem as FolderRow)?.Node;
    }

    private void FolderList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            GoUp();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && FolderList.SelectedItem is FolderRow { Node.IsDirectory: true } row)
        {
            ShowFolder(row.Node);
            e.Handled = true;
        }
    }

    private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: FolderRow { Node.IsDirectory: true } row })
        {
            ShowFolder(row.Node);
            e.Handled = true;
        }
    }

    private void Row_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _contextNode = (sender as FrameworkElement)?.DataContext is FolderRow row ? row.Node : null;
    }

    private void Treemap_NodeClicked(object? sender, ScanNode node) => SelectNode(node);

    private void Treemap_NodeActivated(object? sender, ScanNode node)
    {
        if (node.IsDirectory)
        {
            ShowFolder(node);
            return;
        }

        if (node.Parent is not null)
        {
            ShowFolder(node.Parent);
            Treemap.SelectedNode = node;
            SelectNode(node);
        }
    }

    private void Treemap_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _contextNode = Treemap.HoverNode;

        if (_contextNode is null)
        {
            e.Handled = true;
        }
    }

    private void GoUp()
    {
        if (_current?.Parent is { } parent)
        {
            ShowFolder(parent);
        }
    }

    private void UpButton_Click(object sender, RoutedEventArgs e) => GoUp();

    private void Crumb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BreadcrumbItem item })
        {
            ShowFolder(item.Node);
        }
    }

    // ------------------------------------------------------------------
    // Düğmeler ve menüler
    // ------------------------------------------------------------------

    private void ChooseDisk_Click(object sender, RoutedEventArgs e) =>
        ChooseDiskRequested?.Invoke(this, EventArgs.Empty);

    private async void RescanButton_Click(object sender, RoutedEventArgs e)
    {
        if (_scanPath is not null)
        {
            await StartScanAsync(_scanPath, _expectedBytes);
        }
    }

    private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Multiselect = false };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            await StartScanAsync(dialog.FolderName, DriveHelper.GetUsedBytesIfRoot(dialog.FolderName));
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        _cts?.Cancel();
    }

    private void SkippedText_Click(object sender, MouseButtonEventArgs e)
    {
        if (_result is null || _result.IssueCount == 0)
        {
            return;
        }

        const int maxLines = 30;
        var text = new StringBuilder();

        foreach (var issue in _result.Issues.Take(maxLines))
        {
            var kind = issue.Kind switch
            {
                ScanIssueKind.AccessDenied => Loc.T("IssueAccessDenied"),
                ScanIssueKind.SkippedLink => Loc.T("IssueSkippedLink"),
                _ => Loc.T("IssueIoError")
            };

            text.AppendLine($"• {issue.Path}  —  {kind}");
        }

        var remaining = _result.IssueCount - Math.Min(maxLines, _result.Issues.Count);

        if (remaining > 0)
        {
            text.AppendLine();
            text.AppendLine(Loc.F("SkippedMore", remaining));
        }

        MessageBox.Show(
            text.ToString(),
            Loc.T("SkippedTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void MenuOpenExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (_contextNode is null)
        {
            return;
        }

        try
        {
            var path = _contextNode.GetFullPath();

            var arguments = _contextNode.IsDirectory || _contextNode.IsSmallFilesGroup
                ? $"\"{path}\""
                : $"/select,\"{path}\"";

            Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Explorer açılamadı.");
        }
    }

    private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (_contextNode is null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(_contextNode.GetFullPath());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Yol panoya kopyalanamadı.");
        }
    }

    private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_contextNode is null)
        {
            return;
        }

        if (_contextNode.IsDirectory)
        {
            ShowFolder(_contextNode);
        }
        else if (_contextNode.Parent is not null)
        {
            ShowFolder(_contextNode.Parent);
        }
    }

    // ------------------------------------------------------------------
    // Liste satırı modelleri
    // ------------------------------------------------------------------

    internal sealed class FolderRow
    {
        public FolderRow(ScanNode node, string name, string sizeText, double percent)
        {
            Node = node;
            Name = name;
            SizeText = sizeText;
            PercentValue = percent;
            PercentText = $"{percent:0.#}%";

            if (node.IsDirectory)
            {
                Glyph = "\uE8B7";
                IconBrush = CategoryPalette.FolderIconBrush;
                BarBrush = CategoryPalette.FolderBarBrush;
            }
            else if (node.IsSmallFilesGroup)
            {
                Glyph = "\uE8A5";
                IconBrush = CategoryPalette.SmallFilesBrush;
                BarBrush = CategoryPalette.SmallFilesBrush;
            }
            else
            {
                Glyph = "\uE8A5";
                IconBrush = ExtensionPalette.GetBrush(Path.GetExtension(node.Name), node.Category);
                BarBrush = IconBrush;
            }
        }

        public ScanNode Node { get; }

        public string Name { get; }

        public string SizeText { get; }

        public double PercentValue { get; }

        public string PercentText { get; }

        public string Glyph { get; }

        public Brush IconBrush { get; }

        public Brush BarBrush { get; }
    }

    internal sealed class BreadcrumbItem
    {
        public BreadcrumbItem(ScanNode node, string text, bool isLast)
        {
            Node = node;
            Text = text;
            Weight = isLast ? FontWeights.SemiBold : FontWeights.Normal;
            TextBrush = isLast
                ? (Brush)Application.Current.Resources["TextPrimaryBrush"]
                : (Brush)Application.Current.Resources["TextSecondaryBrush"];
            SeparatorVisibility = isLast ? Visibility.Collapsed : Visibility.Visible;
        }

        public ScanNode Node { get; }

        public string Text { get; }

        public FontWeight Weight { get; }

        public Brush TextBrush { get; }

        public Visibility SeparatorVisibility { get; }
    }

    internal sealed class StatRow
    {
        public StatRow(Brush brush, string name, string sizeText, string percentText)
        {
            Brush = brush;
            Name = name;
            SizeText = sizeText;
            PercentText = percentText;
        }

        public Brush Brush { get; }

        public string Name { get; }

        public string SizeText { get; }

        public string PercentText { get; }
    }
}
