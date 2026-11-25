using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DiskScope.App.Localization;
using Serilog;

namespace DiskScope.App.Views;

public partial class AboutView : UserControl
{
    private const string RepositoryUrl = "https://github.com/CasTiaL06/DiskScope";

    public AboutView()
    {
        InitializeComponent();

        UpdateVersionText();

        LocalizationManager.Instance.PropertyChanged += (_, _) => UpdateVersionText();
    }

    private void UpdateVersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        VersionText.Text = Loc.F("AboutVersion", version);
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Bağlantı açılamadı.");
        }
    }
}
