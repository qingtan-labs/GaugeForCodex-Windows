using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;

namespace GaugeForCodex;

internal static class Ui
{
    internal static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    internal static TextBlock Label(string value, double size = 12, string color = "#62676A") =>
        new() { Text = value, FontSize = size, Foreground = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : Brush(color),
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,8) };
    internal static FrameworkElement Symbol(string name, double size = 16, string color = "#666C6A")
    {
        var data = name switch {
            "close" => "M6,6 L18,18 M18,6 L6,18",
            "refresh" => "M19,8 A8,8 0 1 0 20,15 M19,3 L19,8 L14,8",
            "update" => "M12,15 L12,3 M7,8 L12,3 L17,8 M5,16 L5,21 L19,21 L19,16",
            "settings" => "M4,6 L20,6 M4,12 L20,12 M4,18 L20,18 M8,3 L8,9 M16,9 L16,15 M10,15 L10,21",
            "exit" => "M10,4 L4,4 L4,20 L10,20 M10,12 L21,12 M17,8 L21,12 L17,16",
            "down" => "M6,9 L12,15 L18,9",
            "up" => "M6,15 L12,9 L18,15",
            "clock" => "M12,3 A9,9 0 1 0 12.01,3 M12,7 L12,12 L15,14",
            "widget" => "M3,5 L21,5 L21,19 L3,19 Z M3,10 L21,10 M14,10 L14,19",
            _ => "M6,12 L10,16 L18,8"
        };
        var path = new System.Windows.Shapes.Path { Data = Geometry.Parse(data), Stroke = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : Brush(color),
            StrokeThickness = 1.6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
        var canvas = new Canvas { Width=24, Height=24 }; canvas.Children.Add(path);
        return new Viewbox { Width=size, Height=size, Child=canvas, IsHitTestVisible=false };
    }
    internal static Button Action(string label, Action action, string? icon = null, bool quiet = false)
    {
        var content = new StackPanel { Orientation=Orientation.Horizontal };
        if(icon is not null) { var symbol=Symbol(icon); symbol.Margin=new Thickness(0,0,8,0); content.Children.Add(symbol); }
        content.Children.Add(new TextBlock { Text=label, TextWrapping=TextWrapping.Wrap, VerticalAlignment=VerticalAlignment.Center });
        var button = new Button { Content=content, FontSize=12, Padding=new Thickness(11,9,11,9), Margin=new Thickness(0,3,0,3),
            HorizontalContentAlignment=HorizontalAlignment.Center, Background=SystemParameters.HighContrast ? SystemColors.ControlBrush : Brush(quiet ? "#00FFFFFF" : "#66FFFFFF"),
            Foreground=SystemParameters.HighContrast ? SystemColors.WindowTextBrush : Brush("#424946"),
            BorderBrush=Brush(quiet ? "#00000000" : "#0B26362E"), BorderThickness=new Thickness(1), Cursor=System.Windows.Input.Cursors.Hand,
            MinHeight=34 };
        AutomationProperties.SetName(button,label);
        var frame=new FrameworkElementFactory(typeof(Border)) { Name="Frame" };
        frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));
        foreach(var pair in new[] { (Border.BackgroundProperty,Control.BackgroundProperty), (Border.BorderBrushProperty,Control.BorderBrushProperty),
            (Border.BorderThicknessProperty,Control.BorderThicknessProperty), (Border.PaddingProperty,Control.PaddingProperty) })
            frame.SetValue(pair.Item1,new TemplateBindingExtension(pair.Item2));
        var presenter=new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentProperty,new TemplateBindingExtension(ContentControl.ContentProperty));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);
        frame.AppendChild(presenter);
        var template=new ControlTemplate(typeof(Button)) { VisualTree=frame };
        var hover=new Trigger { Property=UIElement.IsMouseOverProperty,Value=true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty,SystemParameters.HighContrast ? SystemColors.ControlLightBrush : Brush("#142E4437"),"Frame"));
        var pressed=new Trigger { Property=System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty,Value=true };
        pressed.Setters.Add(new Setter(Border.BackgroundProperty,SystemParameters.HighContrast ? SystemColors.ControlDarkBrush : Brush("#23364C3E"),"Frame"));
        var disabled=new Trigger { Property=UIElement.IsEnabledProperty,Value=false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty,0.45,"Frame"));
        template.Triggers.Add(hover); template.Triggers.Add(pressed); template.Triggers.Add(disabled);
        button.FocusVisualStyle=System.Windows.Application.Current.TryFindResource(SystemParameters.FocusVisualStyleKey) as Style;
        button.Template=template; button.Click+=(_,_)=>action(); return button;
    }
    internal static Button IconButton(string icon, string label, Action action)
    {
        var button=Action(label,action,quiet:true); button.Content=Symbol(icon); button.Width=28; button.Height=28;
        button.MinHeight=28; button.Padding=new Thickness(6); button.Margin=new Thickness(0); button.ToolTip=label; return button;
    }
    internal static Image Brand(double size) => new() { Width=size, Height=size, Source=new System.Windows.Media.Imaging.BitmapImage(
        new Uri(System.IO.Path.Combine(AppContext.BaseDirectory,"Assets","app-icon.png"))), Margin=new Thickness(0,0,9,0) };
    internal static Border Track(double remaining, double height=4)
    {
        var track=new Grid { Height=height };
        track.Children.Add(new Border { CornerRadius=new CornerRadius(height/2), Background=SystemParameters.HighContrast ? SystemColors.GrayTextBrush : Brush("#12293D32") });
        var fill=new Border { CornerRadius=new CornerRadius(height/2), Background=SystemParameters.HighContrast ? SystemColors.HighlightBrush : Brush(remaining < 20 ? "#A96259" : remaining < 40 ? "#A48958" : "#537F6A"),
            HorizontalAlignment=HorizontalAlignment.Left };
        track.Children.Add(fill); track.SizeChanged+=(_,_)=>fill.Width=Math.Max(0,track.ActualWidth*Math.Clamp(remaining/100,0,1));
        return new Border { Child=track, Margin=new Thickness(0,8,0,8) };
    }
    internal static Border PeriodRow(QuotaPeriod period, bool glass = false)
    {
        var panel=new StackPanel();
        var line=new Grid(); line.ColumnDefinitions.Add(new ColumnDefinition()); line.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var name=Label(QuotaWindow.PeriodLabel(period),11.5,glass ? "#12251A" : "#334438"); name.Margin=new Thickness(0);
        var amount=Label(Text.Get("remaining",period.RemainingPercent.ToString("0")),11.5,glass ? "#12251A" : "#24372B");
        amount.FontWeight=FontWeights.Medium; amount.Margin=new Thickness(8,0,0,0); Grid.SetColumn(amount,1);
        line.Children.Add(name); line.Children.Add(amount); panel.Children.Add(line);
        panel.Children.Add(Track(period.RemainingPercent,3));
        var reset=Label(QuotaWindow.ResetSummary(period.ResetsAt),11,glass ? "#12251A" : "#334438"); reset.Margin=new Thickness(0);
        panel.Children.Add(reset);
        return new Border { Child=panel, Tag=period, Padding=new Thickness(0,10,0,10), BorderBrush=Brush("#0C203B2C"), BorderThickness=new Thickness(0,1,0,0) };
    }
    internal static void Configure(Window window)
    {
        window.FontFamily=new FontFamily("Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI"); window.FontSize=12;
        window.Background=SystemParameters.HighContrast ? SystemColors.WindowBrush : Brush("#F6F7F3");
        window.UseLayoutRounding=true; System.Windows.Media.TextOptions.SetTextFormattingMode(window,TextFormattingMode.Display);
        var iconPath=System.IO.Path.Combine(AppContext.BaseDirectory,"Assets","app.ico");
        if(System.IO.File.Exists(iconPath)) window.Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath));
    }
}

internal sealed class FlyoutWindow : Window
{
    private readonly StackPanel content=new() { Margin=new Thickness(18,16,18,12) };
    private readonly Border surface;
    private readonly AppController controller;
    internal bool IsSolidSurface => surface.Background is SolidColorBrush brush && brush.Color.A == 255 && surface.Effect is null;
    internal DateTime LastDismissed { get; private set; }
    internal FlyoutWindow(AppController controller)
    {
        this.controller=controller;
        Title="Gauge for Codex · Windows"; Width=336; SizeToContent=SizeToContent.Height;
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; ShowInTaskbar=false; Topmost=false;
        Ui.Configure(this); Background=Brushes.Transparent;
        surface=new Border { Child=new ScrollViewer { Content=content, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled },
            CornerRadius=new CornerRadius(16), BorderThickness=new Thickness(1), BorderBrush=Ui.Brush("#D8DED8"), SnapsToDevicePixels=true };
        Content=surface; NativeGlass.Configure(this, glass:false);
        RenderOptions.SetClearTypeHint(surface,ClearTypeHint.Enabled);
        SourceInitialized+=(_,_)=> { NativeGlass.Remove(this); ApplySurface(); };
        Deactivated+=(_,_)=> { LastDismissed=DateTime.UtcNow; Hide(); };
        PreviewKeyDown+=(_,e)=> { if(e.Key==System.Windows.Input.Key.Escape){ Hide();e.Handled=true; } };
        Refresh();
    }
    internal void Refresh()
    {
        ApplySurface();
        content.Children.Clear();
        var header=new Grid { Margin=new Thickness(0,0,0,16) };
        header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var brand=new StackPanel { Orientation=Orientation.Horizontal };
        brand.Children.Add(Ui.Brand(30));
        var name=new StackPanel { VerticalAlignment=VerticalAlignment.Center };
        var title=Ui.Label("Gauge for Codex",13,"#2D3831"); title.FontWeight=FontWeights.SemiBold; title.Margin=new Thickness(0,0,0,2);
        name.Children.Add(title);
        var subtitle=Ui.Label("Windows · "+Text.Get("title"),10,"#636D62"); subtitle.Margin=new Thickness(0); name.Children.Add(subtitle);
        brand.Children.Add(name); header.Children.Add(brand);
        var close=Ui.IconButton("close",Text.Get("hide"),Hide); Grid.SetColumn(close,1); header.Children.Add(close);
        content.Children.Add(header);
        var quota=controller.Snapshot; var primary=quota?.DisplayPeriod;
        var summary=new StackPanel();
        var meta=new Grid(); meta.ColumnDefinitions.Add(new ColumnDefinition()); meta.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var period=Ui.Label(primary is null ? Text.Get("period") : QuotaWindow.PeriodLabel(primary),11,"#606B61"); period.Margin=new Thickness(0);
        var used=Ui.Label(primary is null ? "" : Text.Get("used",primary.UsedPercent.ToString("0")),10,"#636D62"); used.Margin=new Thickness(8,0,0,0); Grid.SetColumn(used,1);
        meta.Children.Add(period); meta.Children.Add(used); summary.Children.Add(meta);
        var amount=new StackPanel { Orientation=Orientation.Horizontal, Margin=new Thickness(0,8,0,0) };
        var number=Ui.Label(primary is null ? "—" : primary.RemainingPercent.ToString("0")+"%",24,"#26382E");
        number.FontWeight=FontWeights.Medium; number.Margin=new Thickness(0); amount.Children.Add(number);
        var caption=Ui.Label(primary is null ? Text.Get("unknown") : Text.Get("remainingCaption"),11,"#636D63");
        caption.VerticalAlignment=VerticalAlignment.Bottom; caption.Margin=new Thickness(8,0,0,4); amount.Children.Add(caption); summary.Children.Add(amount);
        summary.Children.Add(Ui.Track(primary?.RemainingPercent??0));
        var reset=Ui.Label(primary is null ? Text.Get("manualHint") : QuotaWindow.ResetSummary(primary.ResetsAt),11,"#626D63"); reset.Margin=new Thickness(0);
        reset.ToolTip=primary?.ResetsAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz"); summary.Children.Add(reset);
        var hero=new Border { Child=summary, Padding=new Thickness(14,12,14,12), CornerRadius=new CornerRadius(12),
            Background=SystemParameters.HighContrast ? SystemColors.WindowBrush : Ui.Brush("#FFFFFF"), BorderThickness=new Thickness(1),
            BorderBrush=SystemParameters.HighContrast ? SystemColors.WindowTextBrush : Ui.Brush("#E0E6DF") };
        content.Children.Add(hero);
        if(quota is not null)
        {
            foreach(var p in quota.AdditionalPeriods) content.Children.Add(Ui.PeriodRow(p));
            var stamp=Text.Get("updated",quota.UpdatedAt.ToLocalTime().ToString("MM-dd HH:mm"));
            var updated=Ui.Label(quota.IsManual ? Text.Get("manualValue") : quota.IsCached ? Text.Get("cached",stamp) : stamp,10,"#636D62");
            updated.Margin=new Thickness(2,9,0,12); content.Children.Add(updated);
        }
        else { var unavailable=Ui.Label(Text.Get("noData"),10); unavailable.Margin=new Thickness(2,9,0,12);content.Children.Add(unavailable); }
        var widget=Ui.Action(Text.Get("widget"),()=>controller.SetWidget(!controller.Preferences.WidgetVisible),quiet:true);
        widget.HorizontalContentAlignment=HorizontalAlignment.Stretch; widget.Padding=new Thickness(0,8,0,8); widget.Margin=new Thickness(0);
        AutomationProperties.SetHelpText(widget,Text.Get("widgetHint"));
        var row=new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var labels=new StackPanel(); var label=Ui.Label(Text.Get("widget"),12,"#3F4941"); label.FontWeight=FontWeights.Medium; label.Margin=new Thickness(0,0,0,3); labels.Children.Add(label);
        var hint=Ui.Label(Text.Get("widgetHint"),10,"#636D62");hint.Margin=new Thickness(0);labels.Children.Add(hint);row.Children.Add(labels);
        var switchTrack=new Border { Width=34,Height=20,CornerRadius=new CornerRadius(10), Background=Ui.Brush(controller.Preferences.WidgetVisible ? "#6D917A" : "#C5CBC2"),
            Child=new Border { Width=14,Height=14,CornerRadius=new CornerRadius(7), Background=Brushes.White, Margin=new Thickness(3),
                HorizontalAlignment=controller.Preferences.WidgetVisible ? HorizontalAlignment.Right : HorizontalAlignment.Left }, VerticalAlignment=VerticalAlignment.Center };
        Grid.SetColumn(switchTrack,1);row.Children.Add(switchTrack);widget.Content=row;content.Children.Add(widget);
        content.Children.Add(new Border { Height=1,Background=Ui.Brush("#10283B2F"),Margin=new Thickness(0,10,0,10) });
        var actions=new Grid(); actions.ColumnDefinitions.Add(new ColumnDefinition());actions.ColumnDefinitions.Add(new ColumnDefinition());
        var refresh=Ui.Action(Text.Get("syncShort"),()=>_=controller.Sync(),"refresh");refresh.ToolTip=Text.Get("sync");refresh.Margin=new Thickness(0,0,4,0);
        var updates=Ui.Action(Text.Get(controller.AvailableUpdate is null ? "updatesShort" : "updateAvailable"),()=>_=controller.CheckUpdates(true),"update");
        updates.ToolTip=controller.AvailableUpdate is null ? Text.Get("updates") : Text.Get("newVersion",controller.AvailableUpdate.Version);updates.Margin=new Thickness(4,0,0,0);Grid.SetColumn(updates,1);
        actions.Children.Add(refresh);actions.Children.Add(updates);content.Children.Add(actions);
        var footer=new Grid { Margin=new Thickness(0,6,0,0) };footer.ColumnDefinitions.Add(new ColumnDefinition());footer.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var settings=Ui.Action(Text.Get("settingsShort"),()=>{Hide();controller.ShowSettings();},"settings",true);settings.HorizontalAlignment=HorizontalAlignment.Left;
        var exit=Ui.Action(Text.Get("exit"),()=>controller.Exit(true),"exit",true);exit.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(exit,1);
        footer.Children.Add(settings);footer.Children.Add(exit);content.Children.Add(footer);
    }
    internal void Present()
    {
        Refresh(); MaxHeight=Math.Max(200,SystemParameters.WorkArea.Height-24);Show();UpdateLayout();
        var area=SystemParameters.WorkArea;Left=area.Right-Width-12;Top=Math.Max(area.Top+8,area.Bottom-ActualHeight-10);Activate();
    }
    internal void ApplySurface()
    {
        surface.Background=SystemParameters.HighContrast ? SystemColors.WindowBrush : Ui.Brush("#F6F7F3");
    }
}

internal sealed class SettingsWindow : Window
{
    internal SettingsWindow(AppController controller)
    {
        Title = Text.Get("settings").Trim('…'); Width = 430; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterScreen; Ui.Configure(this);
        var panel = new StackPanel { Margin = new Thickness(22) }; Content = panel;
        panel.Children.Add(Ui.Label("Gauge for Codex · Windows", 17, "#30333B"));
        CheckBox Option(string key, bool value)
        { var check = new CheckBox { Content = Text.Get(key), IsChecked = value, Margin = new Thickness(0,6,0,6) }; panel.Children.Add(check); return check; }
        var follow = Option("follow", controller.Preferences.FollowCodex);
        var login = Option("login", controller.Preferences.StartWithWindows);
        follow.Click += (_, _) => { if (follow.IsChecked == true) login.IsChecked = false; };
        login.Click += (_, _) => { if (login.IsChecked == true) follow.IsChecked = false; };
        var pin = Option("pin", controller.Preferences.Topmost);
        var weekly = Option("weekly", controller.Preferences.WeeklyUpdates);
        panel.Children.Add(Ui.Label(Text.Get("tint"), 11));
        var tint = new Slider { Minimum = PreferenceStore.MinimumReadableTint, Maximum = PreferenceStore.MaximumReadableTint,
            Value = PreferenceStore.ReadableTint(controller.Preferences.GlassTint), Margin = new Thickness(0,4,0,15) }; panel.Children.Add(tint);
        var languages = new[] { "en", "zh", "ja", "es" };
        var language = new ComboBox { ItemsSource = new[] { "English", "简体中文", "日本語", "Español" }, SelectedIndex = Array.IndexOf(languages, Text.Language), Margin = new Thickness(0,0,0,12) }; panel.Children.Add(language);
        panel.Children.Add(Ui.Action(Text.Get("save"), () =>
        {
            var prefs = controller.Preferences; prefs.FollowCodex = follow.IsChecked == true; prefs.StartWithWindows = login.IsChecked == true;
            prefs.Topmost = pin.IsChecked == true; prefs.WeeklyUpdates = weekly.IsChecked == true; prefs.GlassTint = tint.Value;
            prefs.Language = languages[Math.Max(0, language.SelectedIndex)];
            try { controller.ApplySettings(); Close(); }
            catch { System.Windows.MessageBox.Show(Text.Get("settingsFailed"), Title); }
        }));
        panel.Children.Add(Ui.Action(Text.Get("cancel"), () => Close()));
    }
}

internal sealed class ManualWindow : Window
{
    internal QuotaSnapshot? Result { get; private set; }
    internal ManualWindow()
    {
        Title = Text.Get("manual"); Width = 380; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen; Ui.Configure(this);
        var panel = new StackPanel { Margin = new Thickness(22) }; Content = panel;
        panel.Children.Add(Ui.Label(Text.Get("used", "0–100")));
        var used = new TextBox { Text = "0", Padding = new Thickness(8), Margin = new Thickness(0,0,0,14) }; panel.Children.Add(used);
        panel.Children.Add(Ui.Label(Text.Get("resetDate")));
        var reset = new TextBox { Text = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm"), Padding = new Thickness(8), Margin = new Thickness(0,0,0,14) }; panel.Children.Add(reset);
        panel.Children.Add(Ui.Action(Text.Get("save"), () =>
        {
            if (!double.TryParse(used.Text, out var percentage) || !double.IsFinite(percentage) || percentage < 0 || percentage > 100 ||
                !DateTimeOffset.TryParse(reset.Text, out var date) || date <= DateTimeOffset.Now)
            { System.Windows.MessageBox.Show(Text.Get("invalid"), Title); return; }
            Result = new QuotaSnapshot([new QuotaPeriod(percentage, date, 0, "primary")], DateTimeOffset.Now, IsManual: true);
            DialogResult = true;
        }));
        panel.Children.Add(Ui.Action(Text.Get("cancel"), () => { DialogResult = false; }));
    }
}
