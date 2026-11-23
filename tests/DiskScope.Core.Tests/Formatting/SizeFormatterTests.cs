using System.Globalization;
using DiskScope.Core.Formatting;

namespace DiskScope.Core.Tests.Formatting;

public sealed class SizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    public void Format_ReturnsReadableText(long bytes, string expected)
    {
        Assert.Equal(expected, SizeFormatter.Format(bytes, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Format_NegativeValueIsTreatedAsZero()
    {
        Assert.Equal("0 B", SizeFormatter.Format(-5, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Format_UsesGivenCultureDecimalSeparator()
    {
        Assert.Equal("1,5 KB", SizeFormatter.Format(1536, CultureInfo.GetCultureInfo("tr-TR")));
    }
}
