using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace GaugeForCodex;

internal sealed class AppController : IDisposable
{
    internal Preferences Preferences { get; } = PreferenceStore.Read();
    internal QuotaSnapshot? Snapshot { get; private set; }
    internal ReleaseInfo? AvailableUpdate { get; private set; }
    private readonly QuotaClient quota = new();
    private readonly UpdateClient updates = new();
    private readonly Forms.NotifyIcon tray;
    private readonly QuotaWindow widget;
    private readonly FlyoutWindow flyout;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(60) };
    private readonly bool follow;
    private bool syncing, checking, installing;
    private Window? settings, updateWindow;
    private string lastNotifiedVersion = "";
    private int trayPixels;
    internal AppController(bool follow)
    {
        if (PreferenceStore.UpgradeGlassReadability(Preferences)) PreferenceStore.Save(Preferences);
        this.follow = follow; Text.Language = Preferences.Language;
        tray = new Forms.NotifyIcon { Icon = TrayArtwork.Create(false), Text = "Gauge for Codex · Windows", Visible = true };
        trayPixels = tray.Icon.Width;
        widget = new QuotaWindow(Preferences) { HideRequested = () => SetWidget(false) };
        flyout = new FlyoutWindow(this);
        tray.MouseUp += (_, e) => Dispatch(() => { if (e.Button == Forms.MouseButtons.Left) ToggleFlyout(); });
        tray.BalloonTipClicked += (_, _) => Dispatch(() => { if (AvailableUpdate is not null) ShowUpdate(); else flyout.Present(); });
        timer.Tick += (_, _) => { if (TrayArtwork.TaskbarPixels() != trayPixels) UpdateTrayBadge(AvailableUpdate is not null); widget.RefreshCountdown(); _ = Sync(); if (Preferences.WeeklyUpdates && PreferenceStore.IsWeeklyCheckDue(Preferences.LastUpdateCheck, DateTimeOffset.UtcNow)) _ = CheckUpdates(false); };
        Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerMode;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreference;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettings;
        BuildMenus();
    }
    internal void Start(bool interactive)
    {
        Snapshot = Preferences.ManualQuota; widget.SetQuota(Snapshot);
        if (Preferences.WidgetVisible) widget.Show();
        if (interactive) flyout.Present();
        timer.Start(); _ = Sync();
        if (Preferences.WeeklyUpdates && PreferenceStore.IsWeeklyCheckDue(Preferences.LastUpdateCheck,DateTimeOffset.UtcNow)) _ = CheckUpdates(false);
        var first = Path.Combine(AppPaths.Data,"welcomed.txt");
        if (!File.Exists(first))
        { Directory.CreateDirectory(AppPaths.Data); File.WriteAllText(first,"1"); tray.ShowBalloonTip(7000,"Gauge for Codex · Windows",Text.Get("firstRun"),Forms.ToolTipIcon.Info); }
        var result = Path.Combine(AppPaths.Data,"update-result.json");
        if (File.Exists(result))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(result));
                if (doc.RootElement.GetProperty("status").GetString() == "rollback")
                    tray.ShowBalloonTip(6000,"Gauge for Codex",Text.Get("rolledBack"),Forms.ToolTipIcon.Warning);
            }
            catch { }
            File.Delete(result);
        }
    }
    internal void Handle(string command)
    { if (command == "show") flyout.Present(); else if (command == "exit-follow" && follow) Exit(false); }
    private void ToggleFlyout() { if (flyout.IsVisible) flyout.Hide(); else if (DateTime.UtcNow-flyout.LastDismissed > TimeSpan.FromMilliseconds(250)) flyout.Present(); }
    internal async Task WriteUiPreview(string directory)
    {
        Directory.CreateDirectory(directory);
        var iconCases = new List<object>();
        foreach (var pixels in TrayArtwork.Sizes.Where(size => size <= 64))
        foreach (var light in new[] { false, true })
        foreach (var update in new[] { false, true })
        {
            using var icon = TrayArtwork.Create(update, pixels, light);
            if (icon.Width != pixels || icon.Height != pixels)
                throw new InvalidOperationException("Wrong physical tray icon frame: " + pixels);
            using var bitmap = icon.ToBitmap();
            var opaque = 0; var transparent = 0;
            for (var x=0; x<pixels; x++) for (var y=0; y<pixels; y++)
            { var alpha=bitmap.GetPixel(x,y).A; if(alpha==255) opaque++; if(alpha==0) transparent++; }
            if (opaque < pixels*pixels*.15 || transparent < pixels*pixels*.25)
                throw new InvalidOperationException("Faint or opaque tray artwork: " + pixels);
            bitmap.Save(Path.Combine(directory,$"tray-{pixels}-{(light ? "light" : "dark")}-{(update ? "update" : "normal")}.png"));
            iconCases.Add(new { pixels, light, update, opaque, transparent });
        }
        for (var cycle=0; cycle<20; cycle++) UpdateTrayBadge(cycle%2==0);
        if (tray.Icon?.Width != TrayArtwork.TaskbarPixels()) throw new InvalidOperationException("Badge refresh lost native tray size.");
        File.WriteAllText(Path.Combine(directory,"tray-checks.json"),System.Text.Json.JsonSerializer.Serialize(new { taskbarPixels=TrayArtwork.TaskbarPixels(), refreshCycles=20, cases=iconCases }));
        Snapshot = new QuotaSnapshot([new QuotaPeriod(7,DateTimeOffset.Now.AddDays(6),10080,"primary"),new QuotaPeriod(2,DateTimeOffset.Now.AddHours(3),300,"secondary")],DateTimeOffset.Now);
        widget.SetQuota(Snapshot); widget.Show();
        widget.Left=SystemParameters.WorkArea.Right-widget.Width-352;
        widget.Activate(); flyout.Present();
        await Task.Delay(300);
        if (widget.Topmost || flyout.Topmost || widget.ActualHeight < 100 || flyout.ActualHeight < 300) throw new InvalidOperationException("UI smoke assertion failed.");
        if (widget.PeriodName.Text != QuotaWindow.PeriodLabel(Snapshot.DisplayPeriod!) || widget.Remaining.Text != "98%" ||
            widget.PeriodRows.Children.Count != 1 || ((Border)widget.PeriodRows.Children[0]).Tag is not QuotaPeriod { DurationMinutes: 10080 })
            throw new InvalidOperationException("Five-hour main / weekly-only detail assertion failed.");
        if (widget.BrandMark.Child is not System.Windows.Controls.Canvas || Math.Abs(widget.BrandMark.ActualWidth - 19) > .7)
            throw new InvalidOperationException("Component identity must be a small vector, not a raster application tile.");
        var tintTop = ((System.Windows.Media.GradientBrush)widget.Card.Background).GradientStops[0].Color.A;
        if (widget.GlassActive && (tintTop < 142 || tintTop > 178)) throw new InvalidOperationException("Glass tint must preserve both text contrast and backdrop visibility.");
        if (!flyout.IsSolidSurface || (NativeGlass.Supported && System.Windows.Shell.WindowChrome.GetWindowChrome(flyout)?.GlassFrameThickness != new Thickness(0)))
            throw new InvalidOperationException("Tray flyout must be opaque and have no glass frame.");
        if (widget.Effect is not null || widget.Card.Effect is not null || widget.Opacity != 1 || widget.Card.Opacity != 1)
            throw new InvalidOperationException("The text/content layer must not be blurred or faded.");
        Render(widget,"widget-preview.png"); Render(flyout,"tray-panel-preview.png");
        Render(widget,"widget-dark-background-preview.png","#101419");
        Render(widget,"widget-light-background-preview.png","#FFFFFF");
        var collapsedHeight=widget.ActualHeight;
        widget.ToggleDetails(); widget.UpdateLayout();
        if(widget.ActualHeight<=collapsedHeight || widget.DetailsLabel.Text!=Text.Get("detailsClose")) throw new InvalidOperationException("Explicit expansion assertion failed.");
        await Task.Delay(200);
        Render(widget,"widget-expanded-preview.png");
        widget.ToggleDetails(); widget.UpdateLayout();
        if(Math.Abs(widget.ActualHeight-collapsedHeight)>1 || widget.DetailsLabel.Text!=Text.Get("detailsOpen")) throw new InvalidOperationException("Collapse assertion failed.");
        // Live quota may switch from two windows to a single window while open.
        widget.ToggleDetails();
        var dualWindow = Snapshot;
        Snapshot = new QuotaSnapshot([dualWindow.Periods[0]],DateTimeOffset.Now);
        widget.SetQuota(Snapshot); flyout.Refresh(); widget.UpdateLayout();
        if (widget.DetailsButton.Visibility != Visibility.Collapsed || widget.DetailsButton.IsEnabled ||
            widget.Details.Visibility != Visibility.Collapsed || widget.PeriodRows.Children.Count != 0)
            throw new InvalidOperationException("Single window must remove both disclosure and duplicate details.");
        widget.ToggleDetails();
        if (widget.Details.Visibility != Visibility.Collapsed) throw new InvalidOperationException("Card click expanded a single quota window.");
        Render(widget,"single-weekly-preview.png");
        Snapshot = new QuotaSnapshot([dualWindow.Periods[0],dualWindow.Periods[0] with {Kind="secondary"}],DateTimeOffset.Now);
        widget.SetQuota(Snapshot);
        if(widget.DetailsButton.Visibility!=Visibility.Collapsed || widget.PeriodRows.Children.Count!=0)
            throw new InvalidOperationException("Duplicate server fields must not create detail disclosure.");
        Snapshot = dualWindow; widget.SetQuota(Snapshot); flyout.Refresh();
        var originalLanguage=Text.Language;
        foreach(var language in new[]{"en","zh","ja","es"})
        {
            Text.Language=language; widget.ApplyPreferences(); flyout.Refresh(); flyout.UpdateLayout();
            Render(flyout,"panel-"+language+".png");
            Render(widget,"widget-"+language+".png");
            if(flyout.ActualWidth>340 || widget.Remaining.ActualWidth+widget.RemainingCaption.ActualWidth+widget.UsedLabel.ActualWidth+13>270)
                throw new InvalidOperationException("Localized metrics overlap: "+language);
        }
        Text.Language=originalLanguage;
        Snapshot=null;widget.ApplyPreferences();widget.SetQuota(null);flyout.Refresh();
        if(widget.DetailsButton.IsEnabled) throw new InvalidOperationException("Unavailable quota must disable expansion.");
        widget.ToggleDetails(); widget.UpdateLayout();
        if(widget.Details.Visibility!=Visibility.Collapsed) throw new InvalidOperationException("Unavailable quota expanded into empty content.");
        Render(widget,"unavailable-preview.png");Render(flyout,"panel-unavailable.png");
        Snapshot=new QuotaSnapshot([new QuotaPeriod(92,DateTimeOffset.Now.AddHours(2),300,"primary")],DateTimeOffset.Now,IsCached:true);
        widget.SetQuota(Snapshot);flyout.Refresh();Render(widget,"low-quota-preview.png");Render(flyout,"panel-cached.png");
        Snapshot=new QuotaSnapshot([new QuotaPeriod(2,DateTimeOffset.Now.AddHours(3),300,"primary"),new QuotaPeriod(100,DateTimeOffset.Now.AddDays(2),10080,"secondary")],DateTimeOffset.Now);
        widget.SetQuota(Snapshot);flyout.Refresh();
        if(widget.OtherQuotaWarning.Visibility!=Visibility.Visible || widget.Details.Visibility!=Visibility.Collapsed)
            throw new InvalidOperationException("Exhausted weekly quota must remain visible on the collapsed five-hour card.");
        Render(widget,"weekly-exhausted-preview.png");
        flyout.Hide(); SetWidget(true); SetWidget(false);
        if (widget.IsVisible || !tray.Visible) throw new InvalidOperationException("Hide-to-tray assertion failed.");
        File.WriteAllText(Path.Combine(directory,"ui-checks.json"),System.Text.Json.JsonSerializer.Serialize(new { notTopmost=true, hideKeepsTray=true, widgetToggle=true, explicitExpandCollapse=true, noEmptyExpansion=true, fiveHourMainWeeklyDetail=true, singleQuotaHidesDisclosure=true, duplicateQuotaHidesDisclosure=true, weeklyExhaustionWarning=true, componentVector=true, tintTopAlpha=tintTop, solidFlyout=true, noForegroundBlur=true, fourLocaleMetrics=true, unknownAndCachedStates=true, layoutRendered=true, nativeGlassAccepted=widget.GlassActive, glassMode=widget.GlassMode }));
        void Render(Window window,string filename,string matteColor="#EEEFEA")
        {
            window.UpdateLayout(); var width=window.ActualWidth; var height=window.ActualHeight;
            var content = (System.Windows.Media.Visual)window.Content;
            var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(width*2),(int)Math.Ceiling(height*2),192,192,System.Windows.Media.PixelFormats.Pbgra32);
            image.Render(content);
            var matte = new System.Windows.Media.DrawingVisual();
            using(var draw=matte.RenderOpen()) { draw.DrawRectangle(Ui.Brush(matteColor),null,new Rect(0,0,width,height)); draw.DrawImage(image,new Rect(0,0,width,height)); }
            var flattened = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(width*2),(int)Math.Ceiling(height*2),192,192,System.Windows.Media.PixelFormats.Pbgra32); flattened.Render(matte);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(flattened));
            using var file=File.Create(Path.Combine(directory,filename)); encoder.Save(file);
        }
    }
    internal void SetWidget(bool visible)
    {
        Preferences.WidgetVisible = visible; PreferenceStore.Save(Preferences);
        if (visible) { widget.Show(); widget.Activate(); } else widget.Hide();
        BuildMenus(); if (flyout.IsVisible) flyout.Refresh();
    }
    internal async Task Sync()
    {
        if (syncing) return; syncing = true;
        try
        {
            var value = await quota.ReadAsync();
            if (value is not null && (!value.IsCached || Preferences.ManualQuota is null))
            {
                Snapshot = value;
                if (!value.IsCached && Preferences.ManualQuota is not null)
                { Preferences.ManualQuota = null; PreferenceStore.Save(Preferences); }
            }
            else if (Preferences.ManualQuota is not null) Snapshot = Preferences.ManualQuota;
            else if (Snapshot is not null) Snapshot = Snapshot with { IsCached = true };
            widget.SetQuota(Snapshot);
            var tip = Snapshot?.Primary is { } p ? Text.Get("remaining",p.RemainingPercent.ToString("0")) + " · " + QuotaWindow.ResetSummary(p.ResetsAt) : Text.Get("unknown");
            tray.Text = tip[..Math.Min(63,tip.Length)]; if (flyout.IsVisible) flyout.Refresh();
        }
        finally { syncing = false; }
    }
    private void OnPowerMode(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    { if (e.Mode == Microsoft.Win32.PowerModes.Resume) Dispatch(() => _ = Sync()); }
    private void OnUserPreference(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e) => Dispatch(() =>
    { UpdateTrayBadge(AvailableUpdate is not null); widget.ApplyPreferences(); flyout.ApplySurface(); if (flyout.IsVisible) flyout.Refresh(); });
    private void OnDisplaySettings(object? sender, EventArgs e) => Dispatch(() => UpdateTrayBadge(AvailableUpdate is not null));
    private static void Dispatch(Action action) => System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
    internal void ApplySettings()
    {
        PreferenceStore.Save(Preferences); Text.Language = Preferences.Language;
        StartupRegistration.Apply(Preferences); FollowSupervisor.EnsureRunning(); widget.ApplyPreferences(); BuildMenus();
        if (flyout.IsVisible) flyout.Refresh();
    }
    internal void ShowSettings()
    {
        flyout.Hide();
        if (settings is { IsVisible: true }) { settings.Activate(); return; }
        settings = new SettingsWindow(this); settings.Show(); settings.Activate();
    }
    private void Manual()
    {
        flyout.Hide(); var dialog = new ManualWindow();
        if (dialog.ShowDialog() == true && dialog.Result is { } manual)
        { Snapshot = Preferences.ManualQuota = manual; PreferenceStore.Save(Preferences); widget.SetQuota(manual); flyout.Present(); }
    }
    private void BuildMenus()
    {
        var old = tray.ContextMenuStrip; var menu = new Forms.ContextMenuStrip(); var card = new ContextMenu();
        void Add(string key, Action action, bool? check = null)
        {
            var native = new Forms.ToolStripMenuItem(Text.Get(key)) { Checked = check == true };
            native.Click += (_, _) => Dispatch(action); menu.Items.Add(native);
            var item = new MenuItem { Header = Text.Get(key), IsCheckable = check is not null, IsChecked = check == true };
            item.Click += (_, _) => action(); card.Items.Add(item);
        }
        Add("show", () => flyout.Present()); Add("widget", () => SetWidget(!Preferences.WidgetVisible), Preferences.WidgetVisible);
        Add("sync", () => _ = Sync()); Add("manual",Manual); Add("reset",widget.ResetPosition);
        Add("pin", () => { Preferences.Topmost = !Preferences.Topmost; PreferenceStore.Save(Preferences); widget.ApplyPreferences(); BuildMenus(); },Preferences.Topmost);
        menu.Items.Add(new Forms.ToolStripSeparator()); card.Items.Add(new Separator());
        Add("updates", () => _ = CheckUpdates(true)); Add("settings",ShowSettings);
        Add("about", () => System.Windows.MessageBox.Show(Text.Get("aboutText",AppPaths.Version),"Gauge for Codex · Windows"));
        Add("exit", () => Exit(true)); tray.ContextMenuStrip = menu; old?.Dispose(); widget.ContextMenu = card;
    }
    internal async Task CheckUpdates(bool manual)
    {
        if (installing) { if (updateWindow is { IsVisible: true }) updateWindow.Activate(); return; }
        if (checking) return;
        if (manual && AvailableUpdate is not null) { ShowUpdate(); return; }
        checking = true;
        try
        {
            AvailableUpdate = await updates.Check();
            UpdateTrayBadge(AvailableUpdate is not null);
            if (AvailableUpdate is not null)
            {
                if (manual) ShowUpdate();
                else if (lastNotifiedVersion != AvailableUpdate.Version.ToString())
                {
                    lastNotifiedVersion = AvailableUpdate.Version.ToString();
                    tray.ShowBalloonTip(6000,"Gauge for Codex",Text.Get("newVersion",AvailableUpdate.Version),Forms.ToolTipIcon.Info);
                }
            }
            else if (manual) System.Windows.MessageBox.Show(Text.Get("latest",AppPaths.Version),"Gauge for Codex");
            if (flyout.IsVisible) flyout.Refresh();
        }
        catch { if (manual) System.Windows.MessageBox.Show(Text.Get("checkFailed"),"Gauge for Codex"); }
        finally { checking = false; Preferences.LastUpdateCheck = DateTimeOffset.UtcNow; PreferenceStore.Save(Preferences); }
    }
    private void ShowUpdate()
    {
        if (AvailableUpdate is null) return;
        flyout.Hide(); if (updateWindow is { IsVisible: true }) { updateWindow.Activate(); return; }
        var release = AvailableUpdate;
        var window = new Window { Title = Text.Get("newVersion",release.Version), Width = 480, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        Ui.Configure(window); updateWindow = window;
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(Ui.Label(Text.Get("newVersion",release.Version),17,"#30333B"));
        panel.Children.Add(new ScrollViewer { Height = 175, Content = Ui.Label(release.Notes,12), VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        panel.Children.Add(Ui.Label(Text.Get("installWarning"),11));
        var install = Ui.Action(Text.Get("install"), () => { }); panel.Children.Add(install);
        install.Click += async (_, _) =>
        {
            if (installing) return; installing = true; install.IsEnabled = false; install.Content = Text.Get("installing");
            try { var package = await updates.Prepare(release); updates.LaunchInstaller(package,follow); Exit(false); }
            catch { System.Windows.MessageBox.Show(Text.Get("installFailed"),"Gauge for Codex"); installing = false; install.IsEnabled = true; install.Content = Text.Get("install"); }
        };
        window.Closing += (_, e) => { if (installing) e.Cancel = true; };
        window.Content = panel; window.Show(); window.Activate();
    }
    internal void Exit(bool user)
    {
        if (user)
        { Directory.CreateDirectory(AppPaths.Data); File.WriteAllText(AppPaths.Suppression,CodexDesktop.Session()); }
        System.Windows.Application.Current.Shutdown();
    }
    public void Dispose()
    { timer.Stop(); Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerMode; Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreference; Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettings; tray.Visible = false; tray.Icon?.Dispose(); tray.Dispose(); }
    private void UpdateTrayBadge(bool visible)
    {
        var replacement = TrayArtwork.Create(visible);
        trayPixels = replacement.Width;
        var old = tray.Icon; tray.Icon = replacement; old?.Dispose();
    }
}
