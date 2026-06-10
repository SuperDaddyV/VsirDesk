using Xunit;
using UniDesk.Services;
using UniDesk.Helpers;
using System.IO;

namespace UniDesk.Tests;

[Collection("Database Tests")]
public class SettingsServiceTests
{
    private string _testDbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_settings.db");

    private DatabaseService GetDb()
    {
        return new DatabaseService($"Data Source={_testDbFile}");
    }

    private void Cleanup()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_testDbFile))
            {
                File.Delete(_testDbFile);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public async Task GetSetting_ShouldReturnDefaultTheme()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var theme = await settingsService.GetSettingAsync("Theme");
        
        Assert.Equal("System", theme);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_ShouldReturnDefaultWindowOpacity()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var opacity = await settingsService.GetSettingAsync("WindowOpacity");
        
        Assert.Equal("0.70", opacity);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_ShouldReturnDefaultTopMost()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var topMost = await settingsService.GetSettingAsync("TopMost");
        
        Assert.Equal("true", topMost);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_ShouldReturnDefaultPanelWidth()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var panelWidth = await settingsService.GetSettingAsync("PanelWidth");
        
        Assert.Equal("320", panelWidth);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_ShouldReturnDefaultHotkey()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var hotkey = await settingsService.GetSettingAsync("Hotkey");
        
        Assert.Equal("Ctrl+Alt+Space", hotkey);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_ShouldReturnDefaultAutoLocation()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var autoLocation = await settingsService.GetSettingAsync("AutoLocation");
        
        Assert.Equal("true", autoLocation);
        
        Cleanup();
    }

    [Fact]
    public async Task SetSetting_ShouldUpdateValue()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        await settingsService.SetSettingAsync("Theme", "Dark");
        
        var theme = await settingsService.GetSettingAsync("Theme");
        
        Assert.Equal("Dark", theme);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_Generic_ShouldReturnIntValue()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var panelWidth = settingsService.GetSetting<int>("PanelWidth", 0);
        
        Assert.Equal(320, panelWidth);
        
        Cleanup();
    }

    [Fact]
    public async Task GetSetting_Generic_ShouldReturnBoolValue()
    {
        var databaseService = GetDb();
        var settingsService = new SettingsService(databaseService);
        
        await settingsService.InitializeAsync();
        
        var topMost = settingsService.GetSetting<bool>("TopMost", false);
        
        Assert.True(topMost);
        
        Cleanup();
    }
}
