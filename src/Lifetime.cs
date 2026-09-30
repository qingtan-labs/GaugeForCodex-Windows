using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace GaugeForCodex;

internal static class CodexDesktop
{
    internal static string Session()
    {
        var candidates = new List<(int Id, long Start)>();
        foreach (var name in new[] { "ChatGPT", "Codex" })
            foreach (var process in Process.GetProcessesByName(name))
                using (process)
                    try
                    {
                        var path = process.MainModule?.FileName ?? "";
                        var isDesktop = path.Contains("\\OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) ||
                            (name == "Codex" && path.Contains("\\OpenAI\\Codex\\", StringComparison.OrdinalIgnoreCase) &&
                             !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase));
                        if (isDesktop) candidates.Add((process.Id, process.StartTime.ToUniversalTime().Ticks));
                    }
                    catch { }
        var first = candidates.OrderBy(p => p.Start).FirstOrDefault();
        return first.Id == 0 ? "" : $"{first.Id}-{first.Start}";
    }
}

internal static class InstanceMessaging
{
    internal static async Task<bool> Send(string command)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", AppPaths.Pipe, PipeDirection.Out, PipeOptions.Asynchronous);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await pipe.ConnectAsync(timeout.Token);
            using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync(command); await writer.FlushAsync(timeout.Token);
            return true;
        }
        catch { return false; }
    }
    internal static async Task Listen(Action<string> receive, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(AppPaths.Pipe, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(token);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));
                using var reader = new StreamReader(pipe, Encoding.UTF8);
                // The application protocol consists of fixed, bounded commands, never paths or shell text.
                var buffer = new char[32]; var count = await reader.ReadAsync(buffer.AsMemory(), timeout.Token);
                var command = new string(buffer, 0, count).Trim();
                if (command is "show" or "exit-follow") receive(command);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch { }
        }
    }
}

internal static class FollowSupervisor
{
    internal static void EnsureRunning()
    {
        if (PreferenceStore.Read().FollowCodex || PreferenceStore.Read().StartWithWindows)
            Process.Start(new ProcessStartInfo(AppPaths.Exe, "--watch") { UseShellExecute = false, CreateNoWindow = true });
    }
    internal static async Task Run()
    {
        using var mutex = new Mutex(true, "Local\\GaugeForCodex.Follow." + AppPaths.UserKey, out var first);
        if (!first) return;
        var loginHandled = false;
        var prior = "";
        while (true)
        {
            var settings = PreferenceStore.Read();
            if (!settings.FollowCodex && !settings.StartWithWindows) return;
            var session = CodexDesktop.Session();
            if (File.Exists(AppPaths.UpdateMarker))
            {
                // A verified transaction temporarily pauses automatic relaunch.
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(AppPaths.UpdateMarker) < TimeSpan.FromMinutes(15))
                { await Task.Delay(3000); continue; }
            }
            if (!loginHandled && settings.StartWithWindows)
            { Start("--background"); loginHandled = true; }
            if (prior.Length > 0 && session.Length == 0 && settings.FollowCodex)
                await InstanceMessaging.Send("exit-follow");
            if (session.Length > 0 && settings.FollowCodex && !Suppressed(session) && !AppRunning())
                Start("--follow");
            prior = session;
            await Task.Delay(3000);
        }
    }
    private static bool Suppressed(string session)
    { try { return File.ReadAllText(AppPaths.Suppression) == session; } catch { return false; } }
    private static bool AppRunning()
    {
        using var probe = new Mutex(false, "Local\\GaugeForCodex.Card." + AppPaths.UserKey);
        try { if (!probe.WaitOne(0)) return true; probe.ReleaseMutex(); return false; }
        catch (AbandonedMutexException) { probe.ReleaseMutex(); return false; }
    }
    private static void Start(string arguments) =>
        Process.Start(new ProcessStartInfo(AppPaths.Exe, arguments) { UseShellExecute = false, CreateNoWindow = true });
}

internal static class StartupRegistration
{
    internal static void Apply(Preferences preferences)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Gauge for Codex Windows.lnk");
        if (!preferences.FollowCodex && !preferences.StartWithWindows)
        { if (File.Exists(path)) File.Delete(path); return; }
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows shortcut support is unavailable.");
        dynamic shell = Activator.CreateInstance(type)!;
        dynamic shortcut = shell.CreateShortcut(path);
        try
        {
            shortcut.TargetPath = AppPaths.Exe; shortcut.Arguments = "--watch";
            shortcut.WorkingDirectory = AppContext.BaseDirectory; shortcut.IconLocation = AppPaths.Exe + ",0";
            shortcut.Description = "Gauge for Codex · Windows"; shortcut.Save();
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }
}
