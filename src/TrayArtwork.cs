using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using SystemColors = System.Windows.SystemColors;

namespace GaugeForCodex;

// NotifyIcon does not choose another ICO frame after a DPI/theme/update change.
// Always load the exact physical taskbar size, including when drawing a badge.
internal static class TrayArtwork
{
    internal static readonly int[] Sizes = [16, 20, 24, 28, 32, 36, 40, 48, 64, 128, 256];
    internal static int TaskbarPixels()
    {
        try
        {
            var taskbar = FindWindow("Shell_TrayWnd", null);
            var dpi = taskbar == IntPtr.Zero ? 96U : GetDpiForWindow(taskbar);
            var pixels = GetSystemMetricsForDpi(49 /* SM_CXSMICON */, dpi == 0 ? 96U : dpi);
            return pixels > 0 ? Math.Clamp(pixels, 16, 64) : 16;
        }
        catch (EntryPointNotFoundException) { return 16; }
    }
    internal static bool LightTaskbar()
    {
        if (SystemParameters.HighContrast)
            return SystemColors.WindowColor.R + SystemColors.WindowColor.G + SystemColors.WindowColor.B > 384;
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
    }
    internal static Icon Create(bool update, int? physicalPixels = null, bool? lightTaskbar = null)
    {
        var pixels = physicalPixels ?? TaskbarPixels();
        var light = lightTaskbar ?? LightTaskbar();
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", light ? "tray-light.ico" : "tray.ico");
        using var original = new Icon(path, pixels, pixels);
        if (!update) return (Icon)original.Clone();
        // Never render at 32px and then shrink to 16px. The C/prompt retains
        // the same pixel-hinted frame with or without an update notification.
        using var bitmap = new Bitmap(pixels, pixels, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.DrawIcon(original, new Rectangle(0, 0, pixels, pixels));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var diameter = Math.Max(4, (int)Math.Round(pixels * .25));
            var gap = Math.Max(1, (int)Math.Round(pixels / 16.0));
            using var cutout = new SolidBrush(light ? Color.FromArgb(243, 243, 243) : Color.FromArgb(32, 32, 32));
            using var dot = new SolidBrush(light ? Color.FromArgb(153, 89, 21) : Color.FromArgb(245, 187, 92));
            graphics.FillEllipse(cutout, pixels - diameter - gap * 2, 0, diameter + gap * 2, diameter + gap * 2);
            graphics.FillEllipse(dot, pixels - diameter - gap, gap, diameter, diameter);
        }
        var handle = bitmap.GetHicon();
        try
        {
            using var wrapper = Icon.FromHandle(handle);
            return (Icon)wrapper.Clone();
        }
        finally { _ = DestroyIcon(handle); }
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? windowName);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
