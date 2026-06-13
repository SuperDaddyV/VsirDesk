using UniDesk.Models;
using Xunit;

namespace UniDesk.Tests;

public class ModuleSettingsTests
{
    [Fact]
    public void Normalize_ShouldReturnDefaultModules_WhenInputIsMissing()
    {
        var modules = DashboardModuleCatalog.Normalize(null);

        Assert.Equal(
            [
                DashboardModuleIds.TimeWeather,
                DashboardModuleIds.HardwareMonitor,
                DashboardModuleIds.Shortcuts,
                DashboardModuleIds.Todos,
                DashboardModuleIds.QuickNotes,
                DashboardModuleIds.QuickText
            ],
            modules.Select(module => module.ModuleId));
        Assert.All(modules, module => Assert.True(module.IsEnabled));
    }

    [Fact]
    public void Normalize_ShouldFillMissingKnownModules()
    {
        var modules = DashboardModuleCatalog.Normalize(
        [
            new()
            {
                ModuleId = DashboardModuleIds.Todos,
                DisplayName = "待办事项",
                IsEnabled = false,
                SortOrder = 0
            }
        ]);

        Assert.Contains(modules, module => module.ModuleId == DashboardModuleIds.TimeWeather);
        Assert.Contains(modules, module => module.ModuleId == DashboardModuleIds.HardwareMonitor);
        Assert.Contains(modules, module => module.ModuleId == DashboardModuleIds.Shortcuts);
        Assert.Contains(modules, module => module.ModuleId == DashboardModuleIds.Todos && !module.IsEnabled);
        Assert.Contains(modules, module => module.ModuleId == DashboardModuleIds.QuickNotes);
        Assert.Contains(modules, module => module.ModuleId == DashboardModuleIds.QuickText);
        Assert.Equal([0, 1, 2, 3, 4, 5], modules.Select(module => module.SortOrder));
    }

    [Fact]
    public void Normalize_ShouldKeepUnknownModulesSafely()
    {
        var modules = DashboardModuleCatalog.Normalize(
        [
            new()
            {
                ModuleId = "FutureModule",
                DisplayName = "未来模块",
                IsEnabled = true,
                SortOrder = 0
            }
        ]);

        Assert.Contains(modules, module => module.ModuleId == "FutureModule");
        Assert.Equal(modules.Count - 1, modules.Single(module => module.ModuleId == "FutureModule").SortOrder);
    }
}
