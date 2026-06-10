namespace UniDesk.Services;

public interface ITrayService
{
    void Initialize();
    void ShowBalloonTip(string title, string message);
    event Action? TrayIconDoubleClick;
    event Action? SettingsRequested;
    event Action? ExitRequested;
}