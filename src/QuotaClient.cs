using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GaugeForCodex;

public sealed record QuotaPeriod(double UsedPercent, DateTimeOffset? ResetsAt, double DurationMinutes, string Kind)
{
    [JsonIgnore]
    public double RemainingPercent => 100 - UsedPercent;
}

public sealed record QuotaSnapshot(List<QuotaPeriod> Periods, DateTimeOffset UpdatedAt, bool IsCached = false, bool IsManual = false)
{
    [JsonIgnore]
    public QuotaPeriod? Primary => Periods.OrderBy(period => period.RemainingPercent).FirstOrDefault();
    // Presentation is independent of the server's primary/secondary field order.
    // Deduplicate repeated windows; if copies disagree, retain the stricter one.
    [JsonIgnore]
    public IReadOnlyList<QuotaPeriod> DisplayPeriods => Periods
        .DistinctBy(period => (period.UsedPercent, period.ResetsAt, period.DurationMinutes))
        .GroupBy(period => period.DurationMinutes > 0
            ? "minutes:" + period.DurationMinutes.ToString(CultureInfo.InvariantCulture)
            : "kind:" + period.Kind)
        .Select(group => group.OrderBy(period => period.RemainingPercent).First())
        .OrderBy(period => period.DurationMinutes).ToArray();
    [JsonIgnore]
    public QuotaPeriod? DisplayPeriod => DisplayPeriods.FirstOrDefault(period => period.DurationMinutes == 300)
        ?? DisplayPeriods.OrderBy(period => period.RemainingPercent).FirstOrDefault();
    [JsonIgnore]
    public IReadOnlyList<QuotaPeriod> AdditionalPeriods => DisplayPeriods.Where(period => period != DisplayPeriod).ToArray();
    [JsonIgnore]
    public QuotaPeriod? OtherLowPeriod => AdditionalPeriods.Where(period => period.RemainingPercent < 20)
        .OrderBy(period => period.RemainingPercent).FirstOrDefault();
}

public sealed class QuotaClient
{
    private readonly string cachePath = Path.Combine(AppPaths.Data, "quota-windows.json");

    public async Task<QuotaSnapshot?> ReadAsync()
    {
        var live = await ReadLiveAsync();
        if (live is not null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllText(cachePath, JsonSerializer.Serialize(live));
            }
            catch { }
            return live;
        }
        return ReadCache();
    }

    private static async Task<QuotaSnapshot?> ReadLiveAsync()
    {
        Process? process = null;
        try
        {
            var executable = FindCodexExecutable();
            if (executable is null) return null;
            process = Process.Start(new ProcessStartInfo(executable, "app-server")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null) return null;
            // Drain stderr so a diagnostic burst cannot deadlock the stdio protocol. It is not persisted.
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new {
                id=1, method="initialize", @params=new { clientInfo=new { name="gauge-for-codex-windows", title="Gauge for Codex Windows", version=AppPaths.Version }, capabilities=new { experimentalApi=true } }
            }));
            await process.StandardInput.FlushAsync();
            if (!await ReadResponseAsync(process, 1, timeout.Token)) return null;
            await process.StandardInput.WriteLineAsync("{\"method\":\"initialized\",\"params\":{}}");
            await process.StandardInput.WriteLineAsync("{\"id\":2,\"method\":\"account/rateLimits/read\",\"params\":null}");
            await process.StandardInput.FlushAsync();
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
                if (line is null) return null;
                using var json = JsonDocument.Parse(line);
                if (!IsResponse(json.RootElement, 2)) continue;
                if (!json.RootElement.TryGetProperty("result", out var result)) return null;
                var periods = ParseResult(result);
                return periods.Count == 0 ? null : new QuotaSnapshot(periods, DateTimeOffset.Now);
            }
        }
        catch { return null; }
        finally
        {
            try
            {
                if (process is { HasExited: false }) process.Kill(true);
                process?.Dispose();
            }
            catch { }
        }
    }

    internal static List<QuotaPeriod> ParseResult(JsonElement result)
    {
        if (result.ValueKind != JsonValueKind.Object) return [];
        if (result.TryGetProperty("rateLimitsByLimitId", out var byId) && byId.ValueKind == JsonValueKind.Object)
        {
            var buckets = byId.EnumerateObject().Where(item => item.Name.Contains("codex", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Name.Equals("codex", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item => item.Name.Contains("codex", StringComparison.OrdinalIgnoreCase));
            foreach (var bucket in buckets)
            {
                var periods = ParseBucket(bucket.Value);
                if (periods.Count > 0) return periods;
            }
        }
        if (result.TryGetProperty("rateLimits", out var legacy))
        {
            var periods = ParseBucket(legacy);
            if (periods.Count > 0) return periods;
        }
        return ParseBucket(result);
    }

    private static List<QuotaPeriod> ParseBucket(JsonElement bucket)
    {
        if (bucket.ValueKind != JsonValueKind.Object) return [];
        var periods = new List<QuotaPeriod>();
        foreach (var kind in new[] { "primary", "secondary" })
        {
            if (bucket.TryGetProperty(kind, out var value) && ParsePeriod(value, kind) is { } period)
                periods.Add(period);
        }
        if (periods.Count > 0) return periods;
        if (ParsePeriod(bucket, "primary") is { } direct) return [direct];
        foreach (var name in new[] { "rateLimit", "rateLimits", "limit" })
        {
            if (!bucket.TryGetProperty(name, out var nested)) continue;
            periods = ParseBucket(nested);
            if (periods.Count > 0) return periods;
        }
        return [];
    }

    private static QuotaPeriod? ParsePeriod(JsonElement value, string kind)
    {
        if (value.ValueKind != JsonValueKind.Object) return null;
        var used = Number(value, "usedPercent");
        var remaining = Number(value, "remainingPercent");
        if (used is null && remaining is null) return null;
        var percentage = used ?? 100 - remaining!.Value;
        if (!double.IsFinite(percentage)) return null;

        DateTimeOffset? resetsAt = null;
        var reset = Number(value, "resetsAt", "resetAt", "resets_at");
        if (reset is > 0)
        {
            try
            {
                var seconds = reset.Value > 100_000_000_000 ? reset.Value / 1000 : reset.Value;
                resetsAt = DateTimeOffset.FromUnixTimeSeconds((long)seconds);
            }
            catch (ArgumentOutOfRangeException) { }
        }
        else
        {
            foreach (var name in new[] { "resetsAt", "resetAt", "resets_at" })
            {
                if (value.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(item.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    resetsAt = parsed;
                    break;
                }
            }
        }
        var duration = Number(value, "windowDurationMins", "windowDurationMinutes") ??
                       (Number(value, "windowDurationSeconds") ?? 0) / 60;
        if (!double.IsFinite(duration)) duration = 0;
        return new QuotaPeriod(Math.Clamp(percentage, 0, 100), resetsAt, Math.Max(0, duration), kind);
    }

    private static double? Number(JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (!value.TryGetProperty(name, out var item)) continue;
            if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var number)) return number;
            if (item.ValueKind == JsonValueKind.String &&
                double.TryParse(item.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return number;
        }
        return null;
    }

    private QuotaSnapshot? ReadCache()
    {
        try
        {
            if (File.Exists(cachePath))
            {
                var cache = JsonSerializer.Deserialize<QuotaSnapshot>(File.ReadAllText(cachePath));
                if (cache is not null && cache.Periods.Count > 0 &&
                    DateTimeOffset.UtcNow - cache.UpdatedAt < TimeSpan.FromDays(7))
                    return cache with { IsCached = true };
            }
            var oldPath = Path.Combine(AppPaths.Data, "quota-cache.json");
            if (!File.Exists(oldPath)) return null;
            using var json = JsonDocument.Parse(File.ReadAllText(oldPath));
            var root = json.RootElement;
            if (!root.TryGetProperty("UsedPercent", out var usedValue) ||
                !root.TryGetProperty("SavedAt", out var savedValue) ||
                !root.TryGetProperty("ResetsAt", out var resetValue)) return null;
            var savedAt = savedValue.GetDateTimeOffset();
            if (DateTimeOffset.UtcNow - savedAt > TimeSpan.FromDays(7)) return null;
            return new QuotaSnapshot(
                [new QuotaPeriod(Math.Clamp(usedValue.GetDouble(), 0, 100), resetValue.GetDateTimeOffset(), 0, "primary")],
                savedAt, IsCached: true);
        }
        catch { return null; }
    }

    private static async Task<bool> ReadResponseAsync(Process process, int id, CancellationToken token)
    {
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync(token);
            if (line is null) return false;
            using var json = JsonDocument.Parse(line);
            if (!IsResponse(json.RootElement, id)) continue;
            return json.RootElement.TryGetProperty("result", out _);
        }
    }

    private static bool IsResponse(JsonElement root, int id) =>
        root.TryGetProperty("id", out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var actual) && actual == id;

    private static string? FindCodexExecutable()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var binRoot = Path.Combine(localAppData, "OpenAI", "Codex", "bin");
        try
        {
            var installed = Directory.Exists(binRoot)
                ? Directory.EnumerateFiles(binRoot, "codex.exe", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault()
                : null;
            if (installed is not null) return installed;
            var cli = Path.Combine(userProfile, ".codex", "bin", "codex.exe");
            return File.Exists(cli) ? cli : null;
        }
        catch { return null; }
    }
}
