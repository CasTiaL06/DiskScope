using DiskScope.Core.Treemap;

namespace DiskScope.Core.Tests.Treemap;

public sealed class SquarifiedTreemapTests
{
    private static readonly double[] Sizes = [6, 6, 4, 3, 2, 2, 1];
    private static readonly TreemapRect Bounds = new(10, 20, 600, 400);

    [Fact]
    public void Layout_AreasSumToBoundsArea()
    {
        var rects = SquarifiedTreemap.Layout(Sizes, Bounds);

        Assert.Equal(Bounds.Area, rects.Sum(r => r.Area), 3);
    }

    [Fact]
    public void Layout_AreasAreProportionalToSizes()
    {
        var rects = SquarifiedTreemap.Layout(Sizes, Bounds);
        var total = Sizes.Sum();

        for (var i = 0; i < Sizes.Length; i++)
        {
            Assert.Equal(Bounds.Area * Sizes[i] / total, rects[i].Area, 3);
        }
    }

    [Fact]
    public void Layout_AllRectsStayInsideBounds()
    {
        const double tolerance = 1e-6;

        foreach (var rect in SquarifiedTreemap.Layout(Sizes, Bounds))
        {
            Assert.True(rect.X >= Bounds.X - tolerance);
            Assert.True(rect.Y >= Bounds.Y - tolerance);
            Assert.True(rect.Right <= Bounds.Right + tolerance);
            Assert.True(rect.Bottom <= Bounds.Bottom + tolerance);
        }
    }

    [Fact]
    public void Layout_RectsDoNotOverlap()
    {
        var rects = SquarifiedTreemap.Layout(Sizes, Bounds);

        for (var i = 0; i < rects.Length; i++)
        {
            for (var j = i + 1; j < rects.Length; j++)
            {
                Assert.True(Overlap(rects[i], rects[j]) < 1e-6, $"{i} ile {j} çakışıyor");
            }
        }
    }

    [Fact]
    public void Layout_ResultFollowsInputOrder()
    {
        var rects = SquarifiedTreemap.Layout([1, 10, 3], Bounds);

        Assert.True(rects[1].Area > rects[2].Area);
        Assert.True(rects[2].Area > rects[0].Area);
    }

    [Fact]
    public void Layout_NonPositiveSizesGetEmptyRects()
    {
        var rects = SquarifiedTreemap.Layout([5, 0, -3, double.NaN, 5], Bounds);

        Assert.True(rects[1].IsEmpty);
        Assert.True(rects[2].IsEmpty);
        Assert.True(rects[3].IsEmpty);
        Assert.Equal(Bounds.Area, rects[0].Area + rects[4].Area, 3);
    }

    [Fact]
    public void Layout_EmptyInputOrEmptyBounds_ReturnsEmptyRects()
    {
        Assert.Empty(SquarifiedTreemap.Layout([], Bounds));

        var rects = SquarifiedTreemap.Layout([1, 2], new TreemapRect(0, 0, 0, 100));

        Assert.All(rects, r => Assert.True(r.IsEmpty));
    }

    [Fact]
    public void Layout_SingleItemFillsBounds()
    {
        var rects = SquarifiedTreemap.Layout([42], Bounds);

        Assert.Equal(Bounds.Width, rects[0].Width, 6);
        Assert.Equal(Bounds.Height, rects[0].Height, 6);
    }

    [Fact]
    public void Layout_ManyItems_StillConservesArea()
    {
        var random = new Random(12345);
        var sizes = Enumerable.Range(0, 500).Select(_ => random.NextDouble() * 1000 + 1).ToArray();

        var rects = SquarifiedTreemap.Layout(sizes, Bounds);

        Assert.Equal(Bounds.Area, rects.Sum(r => r.Area), 2);
    }

    private static double Overlap(TreemapRect a, TreemapRect b)
    {
        var width = Math.Min(a.Right, b.Right) - Math.Max(a.X, b.X);
        var height = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Y, b.Y);

        return width > 0 && height > 0 ? width * height : 0;
    }
}
