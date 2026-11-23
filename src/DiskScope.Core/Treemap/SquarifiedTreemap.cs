namespace DiskScope.Core.Treemap;

/// <summary>Treemap içindeki bir dikdörtgen (piksel veya herhangi bir birim).</summary>
public readonly record struct TreemapRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public double Area => Width * Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>
/// "Squarified treemap" yerleşimi: boyutlarla orantılı, mümkün olduğunca kareye yakın dikdörtgenler üretir.
/// Arayüzden bağımsızdır ve test edilebilir.
/// </summary>
public static class SquarifiedTreemap
{
    /// <summary>
    /// Verilen boyutlar için dikdörtgenleri hesaplar.
    /// Sonuç, girdiyle aynı sıradadır (sonuç[i], sizes[i] için ayrılan alandır).
    /// Sıfır, negatif veya geçersiz boyutlara boş dikdörtgen verilir.
    /// </summary>
    public static TreemapRect[] Layout(IReadOnlyList<double> sizes, TreemapRect bounds)
    {
        ArgumentNullException.ThrowIfNull(sizes);

        var result = new TreemapRect[sizes.Count];

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return result;
        }

        var order = new List<int>(sizes.Count);
        var total = 0.0;

        for (var i = 0; i < sizes.Count; i++)
        {
            var value = sizes[i];

            if (value > 0 && double.IsFinite(value))
            {
                order.Add(i);
                total += value;
            }
        }

        if (order.Count == 0 || total <= 0)
        {
            return result;
        }

        // En büyük önce. Eşitlikte girdi sırası korunur (sonuç her çalıştırmada aynı olsun).
        order.Sort((a, b) =>
        {
            var byValue = sizes[b].CompareTo(sizes[a]);
            return byValue != 0 ? byValue : a.CompareTo(b);
        });

        var scale = bounds.Width * bounds.Height / total;
        var areas = new double[order.Count];

        for (var k = 0; k < areas.Length; k++)
        {
            areas[k] = sizes[order[k]] * scale;
        }

        var x = bounds.X;
        var y = bounds.Y;
        var width = bounds.Width;
        var height = bounds.Height;
        var start = 0;

        while (start < areas.Length && width > 0 && height > 0)
        {
            var side = Math.Min(width, height);
            var count = 0;
            var sum = 0.0;
            var worst = double.PositiveInfinity;

            // Satıra, en kötü en-boy oranı kötüleşmediği sürece eleman ekle.
            while (start + count < areas.Length)
            {
                var newSum = sum + areas[start + count];
                var newWorst = WorstRatio(areas[start], areas[start + count], newSum, side);

                if (count > 0 && newWorst > worst)
                {
                    break;
                }

                sum = newSum;
                worst = newWorst;
                count++;
            }

            if (width >= height)
            {
                // Sol tarafa dikey bir şerit.
                var stripWidth = sum / height;
                var cursorY = y;

                for (var k = 0; k < count; k++)
                {
                    var itemHeight = areas[start + k] / stripWidth;
                    result[order[start + k]] = new TreemapRect(x, cursorY, stripWidth, itemHeight);
                    cursorY += itemHeight;
                }

                x += stripWidth;
                width -= stripWidth;
            }
            else
            {
                // Üst tarafa yatay bir şerit.
                var stripHeight = sum / width;
                var cursorX = x;

                for (var k = 0; k < count; k++)
                {
                    var itemWidth = areas[start + k] / stripHeight;
                    result[order[start + k]] = new TreemapRect(cursorX, y, itemWidth, stripHeight);
                    cursorX += itemWidth;
                }

                y += stripHeight;
                height -= stripHeight;
            }

            start += count;
        }

        return result;
    }

    private static double WorstRatio(double maxArea, double minArea, double sum, double side)
    {
        var sideSquared = side * side;
        var sumSquared = sum * sum;

        return Math.Max(
            sideSquared * maxArea / sumSquared,
            sumSquared / (sideSquared * minArea));
    }
}
