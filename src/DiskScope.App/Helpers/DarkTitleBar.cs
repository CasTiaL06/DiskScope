using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DiskScope.App.Helpers;

/// <summary>Pencerenin başlık çubuğunu koyu temaya uydurur (Windows 10/11). Desteklenmiyorsa sessizce geçer.</summary>
internal static class DarkTitleBar
{
    private const int UseImmersiveDarkMode = 20;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();

            var dark = 1;
            DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int));

            // Renkler 0x00BBGGRR biçimindedir. #0A0D17 ve #222A4A.
            var caption = 0x00170D0A;
            DwmSetWindowAttribute(handle, CaptionColor, ref caption, sizeof(int));

            var border = 0x004A2A22;
            DwmSetWindowAttribute(handle, BorderColor, ref border, sizeof(int));
        }
        catch (Exception)
        {
            // Görünüm iyileştirmesi; başarısız olursa uygulama çalışmaya devam eder.
        }
    }
}
