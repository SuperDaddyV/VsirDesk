using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LumiDesk.Helpers;

namespace LumiDesk.Windows;

public enum ToastKind
{
    Info,
    Success,
    Warning,
    Error
}

public partial class ToastWindow : Window
{
    private DispatcherTimer? _closeTimer;

    public ToastWindow(string message, ToastKind kind)
    {
        InitializeComponent();
        MessageText.Text = message;
        ApplyKindStyle(kind);
        DesktopWidgetWindowHelper.Configure(this);
    }

    public void ShowWithAutoClose(int durationMs, double stackIndex)
    {
        Opacity = 0;
        Show();
        ToastPlacementHelper.PositionNearAnchor(this, stackIndex: stackIndex);

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });

        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            CloseWithFade();
        };
        _closeTimer.Start();
    }

    private void ApplyKindStyle(ToastKind kind)
    {
        var accent = kind switch
        {
            ToastKind.Success => Color.FromRgb(0x22, 0xC5, 0x5E),
            ToastKind.Warning => Color.FromRgb(0xF5, 0x9E, 0x0B),
            ToastKind.Error => Color.FromRgb(0xEF, 0x44, 0x44),
            _ => Color.FromRgb(0x3B, 0x82, 0xF6)
        };

        AccentBar.Background = new SolidColorBrush(accent);
    }

    private void CloseWithFade()
    {
        var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closeTimer?.Stop();
        base.OnClosed(e);
    }
}
