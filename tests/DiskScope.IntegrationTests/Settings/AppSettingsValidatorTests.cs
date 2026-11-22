using DiskScope.Storage.Settings;

namespace DiskScope.IntegrationTests.Settings;

public class AppSettingsValidatorTests
{
    [Fact]
    public void Validate_WhenSettingsAreValid_ReturnsTrue()
    {
        var settings = new AppSettings
        {
            Language = "en-US",
            Theme = "Dark",
            LastSelectedDrive = "D:\\"
        };

        var result = AppSettingsValidator.Validate(settings);

        Assert.True(result);
        Assert.Equal("en-US", settings.Language);
        Assert.Equal("Dark", settings.Theme);
        Assert.Equal("D:\\", settings.LastSelectedDrive);
    }

    [Fact]
    public void Validate_WhenLanguageIsInvalid_ResetsToDefault()
    {
        var settings = new AppSettings
        {
            Language = "invalid",
            Theme = "Dark"
        };

        var result = AppSettingsValidator.Validate(settings);

        Assert.False(result);
        Assert.Equal("tr-TR", settings.Language);
        Assert.Equal("Dark", settings.Theme);
    }

    [Fact]
    public void Validate_WhenThemeIsInvalid_ResetsToDefault()
    {
        var settings = new AppSettings
        {
            Language = "en-US",
            Theme = "Unknown"
        };

        var result = AppSettingsValidator.Validate(settings);

        Assert.False(result);
        Assert.Equal("en-US", settings.Language);
        Assert.Equal("System", settings.Theme);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("C:")]
    [InlineData("C:/")]
    [InlineData("AB:\\")]
    public void Validate_WhenDriveIsInvalid_ResetsToNull(string drive)
    {
        var settings = new AppSettings
        {
            LastSelectedDrive = drive
        };

        var result = AppSettingsValidator.Validate(settings);

        Assert.False(result);
        Assert.Null(settings.LastSelectedDrive);
    }

    [Fact]
    public void Validate_WhenDriveIsNull_ReturnsTrue()
    {
        var settings = new AppSettings
        {
            LastSelectedDrive = null
        };

        var result = AppSettingsValidator.Validate(settings);

        Assert.True(result);
        Assert.Null(settings.LastSelectedDrive);
    }

    [Fact]
    public void Validate_WhenSettingsIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => AppSettingsValidator.Validate(null!));
    }
}
