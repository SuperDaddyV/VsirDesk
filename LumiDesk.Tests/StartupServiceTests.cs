using LumiDesk.Services;
using Xunit;

namespace LumiDesk.Tests;

public class StartupServiceTests
{
    [Fact]
    public void SyncWithSetting_WhenDisabled_DoesNotThrow()
    {
        var service = new StartupService(new NoOpNotificationService());
        service.SyncWithSetting(false);
        Assert.False(service.IsEnabled);
    }

    private sealed class NoOpNotificationService : INotificationService
    {
        public void ShowInfoMessage(string message) { }
        public void ShowWarningMessage(string message) { }
        public void ShowErrorMessage(string message) { }
        public void ShowSuccessMessage(string message) { }
        public bool ShowConfirmDialog(string message, string title) => false;
    }
}
