using System.IO;
using System.Windows;

namespace GaugeForCodex;

public partial class App : System.Windows.Application
{
    private Mutex? instance;
    private CancellationTokenSource? cancellation;
    private AppController? controller;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--self-test-ui")
        {
            try
            {
                using var test = new AppController(false);
                await test.WriteUiPreview(e.Args[1]); Shutdown(0);
            }
            catch (Exception error)
            {
                Directory.CreateDirectory(e.Args[1]);
                File.WriteAllText(Path.Combine(e.Args[1],"ui-error.txt"),error.ToString());
                Shutdown(1);
            }
            return;
        }
        if (e.Args.Contains("--watch"))
        { await FollowSupervisor.Run(); Shutdown(); return; }
        instance = new Mutex(true, "Local\\GaugeForCodex.Card." + AppPaths.UserKey, out var first);
        if (!first)
        {
            if (!e.Args.Contains("--follow") && !e.Args.Contains("--background")) await InstanceMessaging.Send("show");
            Shutdown(); return;
        }
        cancellation = new();
        controller = new AppController(e.Args.Contains("--follow"));
        _ = InstanceMessaging.Listen(command => Dispatcher.Invoke(() => controller.Handle(command)), cancellation.Token);
        var interactive = !e.Args.Contains("--follow") && !e.Args.Contains("--background");
        controller.Start(interactive);
        FollowSupervisor.EnsureRunning();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        cancellation?.Cancel(); controller?.Dispose();
        if (instance is not null)
        { try { instance.ReleaseMutex(); } catch (ApplicationException) { } instance.Dispose(); }
        base.OnExit(e);
    }
}
