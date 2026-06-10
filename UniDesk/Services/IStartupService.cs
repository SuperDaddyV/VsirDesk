namespace UniDesk.Services;

public interface IStartupService
{
    bool IsEnabled { get; }
    bool Enable();
    bool Disable();
    void SyncWithSetting(bool shouldEnable);
}