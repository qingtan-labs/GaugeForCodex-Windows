using System.IO;
using System.Text.Json;

namespace GaugeForCodex;

public static class AppPaths
{
    public static string Version => typeof(AppPaths).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
        .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion.Split('+')[0] ?? "0.1.0";
    public const string Repository = "qingtan-labs/GaugeForCodex-Windows";
    public static string Data => Path.Combine(AppContext.BaseDirectory, "Data");
    public static string UpdateMarker => Path.Combine(Data, "update-in-progress.json");
    public static string Suppression => Path.Combine(Data, "suppressed-session.txt");
    public static string Exe => Path.Combine(AppContext.BaseDirectory, "GaugeForCodex.exe");
    public static readonly string UserKey = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(Environment.UserDomainName + "\\" + Environment.UserName)))[..16];
    public static string Pipe => "QingtanLabs.GaugeForCodex.Windows." + UserKey;
}

public sealed class Preferences
{
    public bool FollowCodex { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool Topmost { get; set; }
    public bool WidgetVisible { get; set; }
    public string Language { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh") ? "zh" : "en";
    public double GlassTint { get; set; } = 0.58;
    public int GlassStyleVersion { get; set; }
    public double? Left { get; set; }
    public double? Top { get; set; }
    public DateTimeOffset? LastUpdateCheck { get; set; }
    public bool WeeklyUpdates { get; set; } = true;
    public QuotaSnapshot? ManualQuota { get; set; }
}

public static class PreferenceStore
{
    public const double MinimumReadableTint = .56;
    public const double MaximumReadableTint = .70;
    public static double ReadableTint(double value) => double.IsFinite(value)
        ? Math.Clamp(value, MinimumReadableTint, MaximumReadableTint) : .58;
    public static bool UpgradeGlassReadability(Preferences preferences)
    {
        if (preferences.GlassStyleVersion >= 3 && preferences.GlassTint == ReadableTint(preferences.GlassTint)) return false;
        // Retire both previous extremes: 16% washed out text; 80% hid the backdrop.
        preferences.GlassTint = preferences.GlassStyleVersion < 3 &&
            (preferences.GlassTint < MinimumReadableTint || preferences.GlassTint > MaximumReadableTint || !double.IsFinite(preferences.GlassTint))
            ? .58 : ReadableTint(preferences.GlassTint);
        preferences.GlassStyleVersion = 3;
        return true;
    }
    private static string Pathname => Path.Combine(AppPaths.Data, "preferences.json");
    public static Preferences Read()
    {
        try { return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Pathname)) ?? new(); }
        catch { return new(); }
    }
    public static void Save(Preferences preferences)
    {
        Directory.CreateDirectory(AppPaths.Data);
        var temporary = Pathname + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, Pathname, true);
    }
    public static bool IsWeeklyCheckDue(DateTimeOffset? last, DateTimeOffset now) =>
        last is null || now - last.Value >= TimeSpan.FromDays(7) || last.Value > now.AddDays(1);
}
