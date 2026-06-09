using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using LumiDesk.Helpers;

namespace LumiDesk.Services;

public class TrayService : ITrayService, IDisposable
{
    private TaskbarIcon? _notifyIcon;
    private readonly INotificationService _notificationService;

    public event Action? TrayIconDoubleClick;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void Initialize()
    {
        _notifyIcon = new TaskbarIcon
        {
            ToolTipText = "LumiDesk - 桌面侧边助手",
            Icon = AppIconHelper.GetTrayIcon() ?? AppIconHelper.CreateDefaultTrayIcon(),
            Visibility = Visibility.Visible
        };

        _notifyIcon.TrayMouseDoubleClick += (_, _) => TrayIconDoubleClick?.Invoke();
        _notifyIcon.ContextMenu = CreateContextMenu();
    }

    private ContextMenu CreateContextMenu()
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri("Resources/TrayMenu.xaml", UriKind.Relative)
        };

        var menu = new ContextMenu
        {
            Style = resources["TrayContextMenuStyle"] as Style
        };

        menu.Items.Add(CreateMenuItem(resources, "显示 / 隐藏", () => TrayIconDoubleClick?.Invoke()));
        menu.Items.Add(CreateMenuItem(resources, "设置", () => SettingsRequested?.Invoke()));
        menu.Items.Add(new Separator { Style = resources["TraySeparatorStyle"] as Style });
        menu.Items.Add(CreateMenuItem(resources, "退出", () => ExitRequested?.Invoke(), "Danger"));

        return menu;
    }

    private static MenuItem CreateMenuItem(
        ResourceDictionary resources,
        string header,
        Action onClick,
        string? tag = null)
    {
        var item = new MenuItem
        {
            Header = header,
            Style = resources["TrayMenuItemStyle"] as Style,
            Tag = tag
        };
        item.Click += (_, _) => onClick();
        return item;
    }

    public void ShowBalloonTip(string title, string message)
    {
        _notifyIcon?.ShowBalloonTip(title, message, BalloonIcon.Info);
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visibility = Visibility.Collapsed;
            _notifyIcon.Dispose();
        }
    }
}
