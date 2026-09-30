using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace GaugeForCodex;

public sealed record ReleaseInfo(Version Version, string Notes, Uri Package, Uri Checksums, string Filename);

public sealed class UpdateClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };
    public static string Architecture => RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "arm64" : "x64";
    internal static ReleaseInfo? ParseRelease(JsonElement root, string architecture, Version current)
    {
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!TryVersion(tag, out var version) || version <= current) return null;
        var filename = $"GaugeForCodex-Windows-{version}-win-{architecture}.zip";
        Uri? package = null, checksums = null;
        foreach (var item in root.GetProperty("assets").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString();
            if (name != filename && name != "SHA256SUMS.txt") continue;
            if (!Uri.TryCreate(item.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var url) ||
                !AllowedAsset(url, tag, name)) throw new InvalidDataException("Release asset URL is outside the trusted repository.");
            if (name == filename) package = url; else checksums = url;
        }
        if (package is null || checksums is null) throw new InvalidDataException("Release is missing an architecture package or checksums.");
        var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
        return new ReleaseInfo(version!, notes[..Math.Min(6000, notes.Length)], package, checksums, filename);
    }
    internal static bool TryVersion(string tag, out Version? version)
    {
        var clean = tag.StartsWith('v') ? tag[1..] : tag;
        return Version.TryParse(clean, out version) && version.Build >= 0 && version.Revision < 0 &&
            clean == version.ToString(3);
    }
    internal static bool AllowedAsset(Uri url, string tag, string name) => url.Scheme == "https" &&
        url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && url.IsDefaultPort && url.UserInfo.Length == 0 &&
        url.Query.Length == 0 && url.Fragment.Length == 0 &&
        url.AbsolutePath == $"/{AppPaths.Repository}/releases/download/{tag}/{name}";
    public async Task<ReleaseInfo?> Check()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{AppPaths.Repository}/releases/latest");
        request.Headers.UserAgent.ParseAdd($"GaugeForCodex-Windows/{AppPaths.Version}");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        var bytes = await ReadBounded(await response.Content.ReadAsStreamAsync(timeout.Token), 1024 * 1024, timeout.Token);
        using var document = JsonDocument.Parse(bytes);
        return ParseRelease(document.RootElement, Architecture, Version.Parse(AppPaths.Version));
    }
    public async Task<string> Prepare(ReleaseInfo release)
    {
        var folder = Path.Combine(AppPaths.Data, "Updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var zip = Path.Combine(folder, release.Filename);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using (var response = await Http.GetAsync(release.Package, HttpCompletionOption.ResponseHeadersRead, timeout.Token))
        {
            response.EnsureSuccessStatusCode();
            await using var incoming = await response.Content.ReadAsStreamAsync(timeout.Token);
            await using var outgoing = File.Create(zip);
            var buffer = new byte[81920]; long total = 0; int read;
            while ((read = await incoming.ReadAsync(buffer, timeout.Token)) != 0)
            { total += read; if (total > 256L * 1024 * 1024) throw new InvalidDataException("Package is too large."); await outgoing.WriteAsync(buffer.AsMemory(0, read), timeout.Token); }
        }
        using var hashResponse = await Http.GetAsync(release.Checksums, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        hashResponse.EnsureSuccessStatusCode();
        var sums = System.Text.Encoding.UTF8.GetString(await ReadBounded(await hashResponse.Content.ReadAsStreamAsync(timeout.Token), 1024 * 1024, timeout.Token));
        var expected = ChecksumFor(sums, release.Filename);
        await using (var file = File.OpenRead(zip))
        {
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(file, timeout.Token));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA-256 verification failed.");
        }
        var package = Path.Combine(folder, "Package"); Directory.CreateDirectory(package);
        using var archive = ZipFile.OpenRead(zip);
        long expanded = 0;
        foreach (var entry in archive.Entries)
        {
            var destination = SafeEntryPath(package, entry.FullName);
            expanded += entry.Length;
            if (expanded > 1024L * 1024 * 1024 || Path.GetRelativePath(package, destination).Split(Path.DirectorySeparatorChar)[0].Equals("Data", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsafe package contents.");
            if (entry.FullName.EndsWith('/')) { Directory.CreateDirectory(destination); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); entry.ExtractToFile(destination);
        }
        if (new[] { "GaugeForCodex.exe", "GaugeForCodex.dll", "GaugeForCodex.runtimeconfig.json", "Assets/app.ico", "Assets/tray.ico", "Assets/tray-light.ico", "Assets/app-icon.png", "apply-update.ps1" }.Any(name => !File.Exists(Path.Combine(package,name))) ||
            FileVersionInfo.GetVersionInfo(Path.Combine(package,"GaugeForCodex.dll")).ProductVersion?.Split('+')[0] != release.Version.ToString() ||
            File.ReadAllText(Path.Combine(package,"product-id.txt")).Trim() != $"QingtanLabs.GaugeForCodex.Windows|{release.Version}")
            throw new InvalidDataException("Package identity is invalid.");
        return package;
    }
    internal static string SafeEntryPath(string directory, string entry)
    {
        if (entry.Contains(':') || entry.Contains('\\') || entry.StartsWith('/') || string.IsNullOrWhiteSpace(entry))
            throw new InvalidDataException("Unsafe ZIP entry.");
        var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, entry));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("ZIP entry escapes extraction root.");
        return path;
    }
    internal static string ChecksumFor(string sums, string filename)
    {
        foreach (var line in sums.Split('\n'))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[1].TrimStart('*') == filename && parts[0].Length == 64 && parts[0].All(Uri.IsHexDigit))
                return parts[0];
        }
        throw new InvalidDataException("The checksum manifest does not identify this exact package.");
    }
    private static async Task<byte[]> ReadBounded(Stream stream, int max, CancellationToken token)
    {
        await using (stream)
        using (var memory = new MemoryStream())
        {
            var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer, token)) != 0)
            { if (memory.Length + count > max) throw new InvalidDataException("Response is too large."); await memory.WriteAsync(buffer.AsMemory(0,count), token); }
            return memory.ToArray();
        }
    }
    public void LaunchInstaller(string package, bool follow)
    {
        var transaction = Path.GetDirectoryName(package)!;
        var helper = Path.Combine(transaction, "apply-update.ps1");
        File.Copy(Path.Combine(AppContext.BaseDirectory,"apply-update.ps1"), helper);
        Directory.CreateDirectory(AppPaths.Data);
        File.WriteAllText(AppPaths.UpdateMarker, JsonSerializer.Serialize(new { Started = DateTimeOffset.UtcNow, Package = package }));
        try
        {
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"))
            { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", helper, "-InstallDir", AppContext.BaseDirectory.TrimEnd('\\'), "-PackageDir", package,
                "-ParentId", Environment.ProcessId.ToString(), "-RestartMode", follow ? "--follow" : "--background" }) start.ArgumentList.Add(argument);
            if (Process.Start(start) is null) throw new InvalidOperationException("Could not launch the update transaction.");
        }
        catch { File.Delete(AppPaths.UpdateMarker); throw; }
    }
}
