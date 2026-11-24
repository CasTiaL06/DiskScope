using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class FileCategoryClassifierTests
{
    [Theory]
    [InlineData(".mp4", FileCategory.Video)]
    [InlineData(".MP4", FileCategory.Video)]
    [InlineData(".png", FileCategory.Image)]
    [InlineData(".flac", FileCategory.Audio)]
    [InlineData(".pdf", FileCategory.Document)]
    [InlineData(".zip", FileCategory.Archive)]
    [InlineData(".exe", FileCategory.Application)]
    [InlineData(".dll", FileCategory.System)]
    [InlineData(".vhdx", FileCategory.DiskImage)]
    [InlineData(".pak", FileCategory.Game)]
    [InlineData(".ucas", FileCategory.Game)]
    [InlineData(".xyz123", FileCategory.Other)]
    [InlineData("", FileCategory.Other)]
    [InlineData(null, FileCategory.Other)]
    public void Classify_ReturnsExpectedCategory(string? extension, FileCategory expected)
    {
        Assert.Equal(expected, FileCategoryClassifier.Classify(extension));
    }
}
