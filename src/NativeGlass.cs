using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace GaugeForCodex;

internal static class NativeGlass
{
    internal static bool Supported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);
    internal static bool Apply(Window window) => Apply(window, out _);
    internal static bool Apply(Window window, out string mode)
    {
        mode = "opaque-fallback";
        if (!Supported) return false;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return false;
        var target = HwndSource.FromHwnd(handle).CompositionTarget;
        // The UI is tiny and changes at most once per minute. A software WPF surface
        // avoids driver-specific alpha/presentation failures while DWM still provides the blur.
        target.RenderMode = RenderMode.SoftwareOnly;
        target.BackgroundColor = Colors.Transparent;
        var dark = 0; var round = 2; var acrylic = 3; var none = 1;
        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        DwmSetWindowAttribute(handle, 33, ref round, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 38, ref none, sizeof(int));
        _ = TryAccent(handle, 0);
        if (SystemParameters.HighContrast || !TransparencyEnabled()) return false;
        if (DwmExtendFrameIntoClientArea(handle, ref margins) < 0) return false;
        // Compatibility blur has no opaque system Acrylic tint, so our own
        // low-opacity neutral scrim can reveal actual blurred background colors.
        // Accent policy is not a stable public contract: probe dynamically and
        // fall back to the documented DWM Acrylic API on failure.
        if (TryAccent(handle, 3)) { mode = "blur-behind"; return true; }
        if (DwmSetWindowAttribute(handle, 38, ref acrylic, sizeof(int)) >= 0)
        { mode = "system-acrylic"; return true; }
        return false;
    }
    private static bool TransparencyEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("EnableTransparency") is not int value || value != 0;
    }
    private static bool TryAccent(IntPtr handle, int state)
    {
        IntPtr library = IntPtr.Zero, pointer = IntPtr.Zero;
        try
        {
            if (!NativeLibrary.TryLoad("user32.dll", typeof(NativeGlass).Assembly, DllImportSearchPath.System32, out library) ||
                !NativeLibrary.TryGetExport(library, "SetWindowCompositionAttribute", out var entry)) return false;
            var set = Marshal.GetDelegateForFunctionPointer<SetComposition>(entry);
            var policy = new AccentPolicy { State = state };
            pointer = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>());
            Marshal.StructureToPtr(policy, pointer, false);
            var data = new CompositionData { Attribute = 19, Data = pointer, Size = (nuint)Marshal.SizeOf<AccentPolicy>() };
            return set(handle, ref data);
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or ArgumentException) { return false; }
        finally
        {
            if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer);
            if (library != IntPtr.Zero) NativeLibrary.Free(library);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy { public int State; public int Flags; public uint GradientColor; public int AnimationId; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CompositionData { public int Attribute; public IntPtr Data; public nuint Size; }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool SetComposition(IntPtr handle, ref CompositionData data);
    internal static void Remove(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (!Supported || handle == IntPtr.Zero) return;
        var target = HwndSource.FromHwnd(handle).CompositionTarget;
        target.RenderMode = RenderMode.SoftwareOnly;
        target.BackgroundColor = Colors.Transparent;
        var none = 1; var round = 2; var margins = new Margins();
        _ = TryAccent(handle, 0);
        _ = DwmSetWindowAttribute(handle, 38, ref none, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 33, ref round, sizeof(int));
        _ = DwmExtendFrameIntoClientArea(handle, ref margins);
    }
    internal static void Configure(Window window, bool glass = true)
    {
        if (!Supported) { window.AllowsTransparency = true; return; }
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = 0, ResizeBorderThickness = new Thickness(0),
            GlassFrameThickness = new Thickness(glass ? -1 : 0), UseAeroCaptionButtons = false,
            CornerRadius = new CornerRadius(16)
        });
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Margins { public int Left; public int Right; public int Top; public int Bottom; }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr handle, ref Margins margins);
}
