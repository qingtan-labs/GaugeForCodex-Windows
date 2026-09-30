using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace GaugeForCodex;

public partial class QuotaWindow : Window
{
    private readonly Preferences preferences;
    private QuotaSnapshot? snapshot;
    private bool expanded, pointerDown, pointerMoved, nativeGlass;
    private double progressFraction, leftStart, topStart, dpiX = 1, dpiY = 1;
    private System.Drawing.Point pointerStart;
    public Action? HideRequested { get; set; }
    internal bool GlassActive => nativeGlass;
    internal string GlassMode { get; private set; } = "opaque-fallback";

    public QuotaWindow(Preferences preferences)
    {
        this.preferences = preferences;
        InitializeComponent();
        Ui.Configure(this); Background = System.Windows.Media.Brushes.Transparent;
        NativeGlass.Configure(this);
        SourceInitialized += (_, _) => ApplyPreferences();
        Loaded += (_, _) => RestorePosition();
        SizeChanged += (_, _) => { if (IsLoaded) KeepVisible(); };
        Card.LostMouseCapture += (_, _) => pointerDown = false;
    }

    public void ApplyPreferences()
    {
        nativeGlass = NativeGlass.Apply(this, out var mode); GlassMode = mode;
        Topmost = preferences.Topmost;
        TitleLabel.Text = Text.Get("title");
        HideButton.ToolTip = Text.Get("hide");
        MenuButton.ToolTip = Text.Get("settings");
        Card.ToolTip = Text.Get("tooltip");
        RemainingCaption.Text = Text.Get("remainingCaption");
        DetailsLabel.Text = Text.Get(expanded ? "detailsClose" : "detailsOpen");
        DetailsButton.ToolTip = DetailsLabel.Text;
        System.Windows.Automation.AutomationProperties.SetName(HideButton, Text.Get("hide"));
        System.Windows.Automation.AutomationProperties.SetName(MenuButton, Text.Get("settings"));
        var alpha = SystemParameters.HighContrast ? 255 : nativeGlass
            ? (int)(PreferenceStore.ReadableTint(preferences.GlassTint) * 255)
            : 242;
        Card.Background = SystemParameters.HighContrast ? SystemColors.WindowBrush : new LinearGradientBrush(
            System.Windows.Media.Color.FromArgb((byte)alpha, 248, 249, 247),
            System.Windows.Media.Color.FromArgb((byte)Math.Min(255, alpha + 6), 241, 244, 239), 90);
        if (SystemParameters.HighContrast)
            foreach (var label in new[] { TitleLabel, PeriodName, Remaining, RemainingCaption, UsedLabel, ResetLine, SourceLine, DetailsLabel, OtherQuotaWarning })
                label.Foreground = SystemColors.WindowTextBrush;
        SetQuota(snapshot);
    }

    public void ResetPosition()
    {
        preferences.Left = preferences.Top = null;
        PlaceDefault();
        PreferenceStore.Save(preferences);
    }
    private void PlaceDefault()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 16;
        Top = area.Bottom - Math.Max(116, ActualHeight) - 14;
    }
    private void RestorePosition()
    {
        if (preferences.Left is not { } left || preferences.Top is not { } top || !double.IsFinite(left) || !double.IsFinite(top) ||
            left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 48 ||
            left + Width < SystemParameters.VirtualScreenLeft + 48 ||
            top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 48 ||
            top + ActualHeight < SystemParameters.VirtualScreenTop + 48)
        { PlaceDefault(); return; }
        Left = left; Top = top; KeepVisible();
    }
    private void KeepVisible()
    {
        if (preferences.Left is null) { PlaceDefault(); return; }
        var area = SystemParameters.WorkArea;
        if (Left >= area.Left && Left < area.Right && Top < area.Bottom && Top + ActualHeight > area.Bottom - 8)
        { Top = area.Bottom - ActualHeight - 8; SavePosition(); }
    }
    private void SavePosition()
    {
        preferences.Left = Left; preferences.Top = Top;
        try { PreferenceStore.Save(preferences); } catch { }
    }

    public void SetQuota(QuotaSnapshot? value)
    {
        snapshot = value;
        PeriodRows.Children.Clear();
        var primary = value?.DisplayPeriod;
        var additional = value?.AdditionalPeriods ?? [];
        var canExpand = additional.Count > 0;
        if (!canExpand && expanded) ToggleDetails();
        DetailsButton.IsEnabled = canExpand;
        DetailsButton.Visibility = canExpand ? Visibility.Visible : Visibility.Collapsed;
        DetailsButton.ToolTip = canExpand ? DetailsLabel.Text : null;
        Card.ToolTip = Text.Get(canExpand ? "tooltip" : "tooltipSingle");
        OtherQuotaWarning.Visibility = Visibility.Collapsed;
        OtherQuotaWarning.Text = "";
        if (primary is null)
        {
            Remaining.Text = "—"; RemainingCaption.Text = Text.Get("unknown"); PeriodName.Text = UsedLabel.Text = "";
            ResetLine.Text = Text.Get("manualHint"); SourceLine.Text = Text.Get("noData");
            progressFraction = 0; UpdateProgressWidth(); return;
        }
        Remaining.Text = primary.RemainingPercent.ToString("0") + "%";
        RemainingCaption.Text = Text.Get("remainingCaption");
        PeriodName.Text = PeriodLabel(primary);
        UsedLabel.Text = Text.Get("used", primary.UsedPercent.ToString("0"));
        ResetLine.Text = ResetSummary(primary.ResetsAt);
        ResetLine.ToolTip = primary.ResetsAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? Text.Get("resetUnknown");
        progressFraction = Math.Clamp(primary.RemainingPercent / 100, 0, 1);
        ProgressFill.Background = SystemParameters.HighContrast ? SystemColors.HighlightBrush : Brush(primary.RemainingPercent < 20 ? "#A96259" : primary.RemainingPercent < 40 ? "#A48958" : "#537F6A");
        UpdateProgressWidth();
        foreach (var period in additional)
        {
            PeriodRows.Children.Add(Ui.PeriodRow(period, glass: true));
        }
        if (value!.OtherLowPeriod is { } low)
        {
            OtherQuotaWarning.Text = low.RemainingPercent <= 0 ? Text.Get("otherExhausted", PeriodLabel(low))
                : Text.Get("otherQuotaWarning", PeriodLabel(low), low.RemainingPercent.ToString("0"));
            OtherQuotaWarning.Visibility = Visibility.Visible;
        }
        var updated = Text.Get("updated", value.UpdatedAt.ToLocalTime().ToString("MM-dd HH:mm"));
        SourceLine.Text = value.IsManual ? Text.Get("manualValue") : value.IsCached ? Text.Get("cached", updated) : updated;
    }
    public void RefreshCountdown() { if (snapshot is not null) SetQuota(snapshot); }
    internal static string PeriodLabel(QuotaPeriod period) => period.DurationMinutes >= 1440
        ? Text.Get("dayPeriod", Math.Round(period.DurationMinutes / 1440))
        : period.DurationMinutes >= 60 ? Text.Get("hourPeriod", Math.Round(period.DurationMinutes / 60))
        : period.DurationMinutes > 0 ? Text.Get("minutePeriod", period.DurationMinutes) : Text.Get("period");
    internal static string ResetSummary(DateTimeOffset? reset)
    {
        if (reset is null) return Text.Get("resetUnknown");
        var local = reset.Value.ToLocalTime(); var now = DateTimeOffset.Now;
        if (local <= now) return Text.Get("expired");
        var days = (local.Date - now.Date).Days;
        return days == 0 ? Text.Get("today", local.ToString("HH:mm")) : days == 1
            ? Text.Get("tomorrow", local.ToString("HH:mm"))
            : Text.Get("days", days, local.ToString("MM-dd HH:mm"));
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        for (var item = e.OriginalSource as DependencyObject; item is not null && item != Card; item = VisualTreeHelper.GetParent(item))
            if (item is System.Windows.Controls.Primitives.ButtonBase) return;
        if (e.ChangedButton != MouseButton.Left) return;
        pointerDown = true; pointerMoved = false; pointerStart = Forms.Cursor.Position;
        leftStart = Left; topStart = Top; var dpi = VisualTreeHelper.GetDpi(this);
        dpiX = dpi.DpiScaleX; dpiY = dpi.DpiScaleY; Card.CaptureMouse(); e.Handled = true;
    }
    private void Card_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!pointerDown || e.LeftButton != MouseButtonState.Pressed) return;
        var current = Forms.Cursor.Position; var dx = current.X - pointerStart.X; var dy = current.Y - pointerStart.Y;
        if (!pointerMoved && Math.Abs(dx) < 5 && Math.Abs(dy) < 5) return;
        pointerMoved = true; Left = leftStart + dx / dpiX; Top = topStart + dy / dpiY; e.Handled = true;
    }
    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!pointerDown || e.ChangedButton != MouseButton.Left) return;
        pointerDown = false; Card.ReleaseMouseCapture();
        if (pointerMoved) SavePosition();
        else ToggleDetails();
        e.Handled = true;
    }
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    { if (ContextMenu is { } menu) { menu.PlacementTarget = MenuButton; menu.IsOpen = true; } }
    private void HideButton_Click(object sender, RoutedEventArgs e) => HideRequested?.Invoke();
    private void DetailsButton_Click(object sender, RoutedEventArgs e) { ToggleDetails(); e.Handled = true; }
    internal void ToggleDetails()
    {
        if (!expanded && snapshot?.AdditionalPeriods.Count is not > 0) return;
        expanded = !expanded; Details.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        DetailsLabel.Text = Text.Get(expanded ? "detailsClose" : "detailsOpen");
        DetailsButton.ToolTip = DetailsLabel.Text;
        DetailsChevron.Data = Geometry.Parse(expanded ? "M2,8 L6,4 L10,8" : "M2,4 L6,8 L10,4");
        if (expanded && SystemParameters.ClientAreaAnimation)
            Details.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(0,1,TimeSpan.FromMilliseconds(140)));
    }
    private void ProgressTrack_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateProgressWidth();
    private void UpdateProgressWidth() => ProgressFill.Width = Math.Max(0, ProgressTrack.ActualWidth * progressFraction);
    private static SolidColorBrush Brush(string color) => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
}
