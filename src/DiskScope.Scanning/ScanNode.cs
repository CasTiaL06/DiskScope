namespace DiskScope.Scanning;

/// <summary>
/// Tarama ağacındaki bir düğüm: klasör, büyük dosya veya "küçük dosyalar" grubu.
/// Bellek kontrolü için küçük dosyalar tek tek değil, klasör başına tek bir grup olarak tutulur.
/// </summary>
public sealed class ScanNode
{
    private List<ScanNode>? _children;

    public ScanNode(string name, bool isDirectory, ScanNode? parent, long size = 0)
    {
        Name = name;
        IsDirectory = isDirectory;
        Parent = parent;
        Size = size;

        if (!isDirectory)
        {
            FileCount = 1;
        }
    }

    /// <summary>Klasör/dosya adı. Kök düğümde tam yol (örneğin "C:\").</summary>
    public string Name { get; }

    public bool IsDirectory { get; }

    /// <summary>Bir klasördeki küçük dosyaların toplandığı sanal düğüm mü?</summary>
    public bool IsSmallFilesGroup { get; init; }

    public ScanNode? Parent { get; }

    /// <summary>Toplam boyut (bayt). Klasörde alt öğelerin toplamıdır.</summary>
    public long Size { get; internal set; }

    /// <summary>Bu düğümün altındaki toplam dosya sayısı.</summary>
    public long FileCount { get; internal set; }

    /// <summary>Bu düğümün altındaki toplam klasör sayısı (kendisi hariç).</summary>
    public long FolderCount { get; internal set; }

    public FileCategory Category { get; init; }

    /// <summary>Alt öğeler, büyükten küçüğe sıralıdır.</summary>
    public IReadOnlyList<ScanNode> Children =>
        _children ?? (IReadOnlyList<ScanNode>)Array.Empty<ScanNode>();

    internal void AddChild(ScanNode child)
    {
        _children ??= new List<ScanNode>();
        _children.Add(child);
    }

    /// <summary>Alt öğeler tamamlandıktan sonra toplamları hesaplar ve sıralar.</summary>
    internal void Complete()
    {
        Size = 0;
        FileCount = 0;
        FolderCount = 0;

        if (_children is null)
        {
            return;
        }

        foreach (var child in _children)
        {
            Size += child.Size;
            FileCount += child.FileCount;

            if (child.IsDirectory)
            {
                FolderCount += child.FolderCount + 1;
            }
        }

        _children.Sort((a, b) => b.Size.CompareTo(a.Size));
        _children.TrimExcess();
    }

    /// <summary>Düğümün diskteki tam yolunu üretir ("küçük dosyalar" grubu için içinde bulunduğu klasör).</summary>
    public string GetFullPath()
    {
        var parts = new Stack<string>();

        for (var node = this; node is not null; node = node.Parent)
        {
            parts.Push(node.Name);
        }

        return Path.Combine(parts.ToArray());
    }
}
