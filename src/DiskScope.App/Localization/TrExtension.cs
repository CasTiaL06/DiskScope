using System.Windows;
using System.Windows.Data;

namespace DiskScope.App.Localization;

public sealed class TrExtension : Binding
{
    private string _key = string.Empty;

    public string Key
    {
        get => _key;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            _key = value;
            Source = LocalizationManager.Instance;
            Path = new PropertyPath($"[{value}]");
            Mode = BindingMode.OneWay;
        }
    }
}
