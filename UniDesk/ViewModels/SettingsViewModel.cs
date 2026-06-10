using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniDesk.Helpers;
using UniDesk.Services;

namespace UniDesk.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private readonly INotificationService _notificationService;
    private readonly ILayoutService _layoutService;
    private readonly IWeatherService _weatherService;
    private readonly IStartupService _startupService;
    private readonly ITodoBackupService _todoBackupService;
    private readonly MainWindowViewModel _mainWindowViewModel;

    private readonly Dictionary<string, string> _originalSettings = new();
    private bool _isLoading;

    [ObservableProperty]
    private string _selectedColorScheme = AppColorSchemeCatalog.DefaultSchemeId;

    [ObservableProperty]
    private bool _startupEnabled;

    [ObservableProperty]
    private double _windowOpacity;

    [ObservableProperty]
    private double _panelWidth;

    [ObservableProperty]
    private string _weatherApiKey = string.Empty;

    [ObservableProperty]
    private string _weatherApiHost = string.Empty;

    [ObservableProperty]
    private bool _isEditingWeatherApi;

    [ObservableProperty]
    private int _shortcutMaxCount = ShortcutLimitHelper.DefaultLimit;

    public ObservableCollection<ColorSchemeOptionViewModel> ColorSchemes { get; } = new();

    public bool LastSaveSucceeded { get; private set; }

    public bool PendingWeatherSettingsChanged { get; private set; }

    public string PendingApiKey { get; private set; } = string.Empty;

    public string PendingApiHost { get; private set; } = string.Empty;

    public SettingsViewModel(
        ISettingsService settingsService,
        IWindowService windowService,
        INotificationService notificationService,
        ILayoutService layoutService,
        IWeatherService weatherService,
        IStartupService startupService,
        ITodoBackupService todoBackupService,
        MainWindowViewModel mainWindowViewModel)
    {
        _settingsService = settingsService;
        _windowService = windowService;
        _notificationService = notificationService;
        _layoutService = layoutService;
        _weatherService = weatherService;
        _startupService = startupService;
        _todoBackupService = todoBackupService;
        _mainWindowViewModel = mainWindowViewModel;

        foreach (var scheme in AppColorSchemeCatalog.All)
        {
            ColorSchemes.Add(new ColorSchemeOptionViewModel(scheme));
        }

        LoadSettings();
    }

    private void LoadSettings()
    {
        _isLoading = true;
        try
        {
            SelectedColorScheme = AppColorSchemeCatalog.NormalizeId(
                _settingsService.GetValue("ColorScheme", _settingsService.GetValue("Theme", AppColorSchemeCatalog.DefaultSchemeId)));

            StartupEnabled = ReadStartupSetting();
            WindowOpacity = _settingsService.GetSetting("WindowOpacity", 0.70);
            PanelWidth = _settingsService.GetSetting("PanelWidth", 320.0);
            WeatherApiKey = _settingsService.GetValue("WeatherApiKey", "");
            WeatherApiHost = _settingsService.GetValue("WeatherApiHost", "");
            ShortcutMaxCount = ShortcutLimitHelper.ParseLimit(
                _settingsService.GetValue("ShortcutMaxCount", ShortcutLimitHelper.DefaultLimit.ToString()));
        }
        finally
        {
            _isLoading = false;
        }

        UpdateColorSchemeSelection();
        AppColorSchemeCatalog.Apply(SelectedColorScheme);
        SaveOriginalSettings();
    }

    private void SaveOriginalSettings()
    {
        _originalSettings["ColorScheme"] = SelectedColorScheme;
        _originalSettings["Startup"] = StartupEnabled.ToString();
        _originalSettings["WindowOpacity"] = WindowOpacity.ToString(CultureInfo.InvariantCulture);
        _originalSettings["PanelWidth"] = PanelWidth.ToString(CultureInfo.InvariantCulture);
        _originalSettings["WeatherApiKey"] = WeatherApiKey;
        _originalSettings["WeatherApiHost"] = WeatherApiHost;
        _originalSettings["ShortcutMaxCount"] = ShortcutMaxCount.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void SelectColorScheme(string? schemeId)
    {
        if (string.IsNullOrWhiteSpace(schemeId))
        {
            return;
        }

        SelectedColorScheme = AppColorSchemeCatalog.NormalizeId(schemeId);
    }

    [RelayCommand]
    private void ToggleWeatherApiEdit() => IsEditingWeatherApi = !IsEditingWeatherApi;

    [RelayCommand]
    private void SelectShortcutLimit(string? limitText)
    {
        if (!int.TryParse(limitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit)
            || !ShortcutLimitHelper.AllowedLimits.Contains(limit))
        {
            return;
        }

        ShortcutMaxCount = limit;
    }

    partial void OnSelectedColorSchemeChanged(string value)
    {
        UpdateColorSchemeSelection();
        if (!_isLoading)
        {
            AppColorSchemeCatalog.Apply(value);
        }
    }

    partial void OnWindowOpacityChanged(double value) => ApplyWindowPreview();

    partial void OnPanelWidthChanged(double value) => ApplyWindowPreview();

    partial void OnShortcutMaxCountChanged(int value) =>
        _mainWindowViewModel.SetShortcutLimitPreview(value);

    private void UpdateColorSchemeSelection()
    {
        foreach (var option in ColorSchemes)
        {
            option.IsSelected = string.Equals(option.Id, SelectedColorScheme, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ApplyWindowPreview()
    {
        if (_isLoading)
        {
            return;
        }

        _windowService.SetOpacity(WindowOpacity);
        _windowService.SetWidth(PanelWidth);
        _mainWindowViewModel.WindowOpacity = WindowOpacity;
        _mainWindowViewModel.PanelWidth = PanelWidth;
    }

    [RelayCommand]
    private async Task Save()
    {
        LastSaveSucceeded = false;
        var weatherSettingsChanged = false;
        var apiKeyToValidate = string.Empty;
        var apiHostToValidate = string.Empty;

        try
        {
            apiKeyToValidate = WeatherApiKey.Trim();
            apiHostToValidate = QWeatherApiClient.NormalizeHost(WeatherApiHost.Trim());
            weatherSettingsChanged =
                _originalSettings.GetValueOrDefault("WeatherApiKey") != apiKeyToValidate ||
                _originalSettings.GetValueOrDefault("WeatherApiHost") != apiHostToValidate;

            _settingsService.SetValue("ColorScheme", SelectedColorScheme);
            _settingsService.SetValue("Theme", SelectedColorScheme);
            _settingsService.SetValue("Startup", StartupEnabled.ToString());
            _settingsService.SetValue("WindowOpacity", WindowOpacity.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue("PanelWidth", PanelWidth.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue("WeatherApiKey", apiKeyToValidate);
            _settingsService.SetValue("WeatherApiHost", apiHostToValidate);
            _settingsService.SetValue("ShortcutMaxCount", ShortcutMaxCount.ToString(CultureInfo.InvariantCulture));

            await _settingsService.FlushPendingSavesAsync();

            AppColorSchemeCatalog.Apply(SelectedColorScheme);
            ApplyWindowPreview();
            _mainWindowViewModel.SetShortcutLimitPreview(null);
            await _mainWindowViewModel.ReloadShortcutsAsync();
            ApplyStartupSetting();
            SaveOriginalSettings();

            PendingApiKey = apiKeyToValidate;
            PendingApiHost = apiHostToValidate;
            PendingWeatherSettingsChanged = weatherSettingsChanged;
            LastSaveSucceeded = true;
            IsEditingWeatherApi = false;
        }
        catch (Exception ex)
        {
            RevertToOriginalSettings();
            _notificationService.ShowErrorMessage($"保存设置失败：{ex.Message}");
            return;
        }

        RequestClose?.Invoke(this, true);
    }

    public async Task CompleteSaveFollowUpAsync(
        string apiKeyToValidate,
        string apiHostToValidate,
        bool weatherSettingsChanged)
    {
        if (!LastSaveSucceeded)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(apiKeyToValidate))
            {
                var validation = await _weatherService.ValidateApiKeyAsync(apiKeyToValidate, apiHostToValidate);
                if (!validation.IsValid)
                {
                    _notificationService.ShowWarningMessage(
                        string.IsNullOrWhiteSpace(validation.Message)
                            ? "和风天气凭据校验失败，请检查 API Host 与 API Key"
                            : validation.Message);
                }
            }

            if (weatherSettingsChanged)
            {
                await _mainWindowViewModel.RefreshWeatherAfterSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowErrorMessage($"应用天气设置失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var result = _notificationService.ShowConfirmDialog("确定要恢复默认设置吗？这将重置所有设置为默认值。", "确认恢复");
        if (!result) return;

        SelectedColorScheme = AppColorSchemeCatalog.DefaultSchemeId;
        StartupEnabled = false;
        WindowOpacity = 0.70;
        PanelWidth = 320;
        WeatherApiKey = "";
        WeatherApiHost = "";
        IsEditingWeatherApi = false;
        ShortcutMaxCount = ShortcutLimitHelper.DefaultLimit;

        _notificationService.ShowInfoMessage("已恢复默认设置，点击保存后生效");
    }

    [RelayCommand]
    private async Task BackupTodosAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "备份待办事项",
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = $"UniDesk-todos-{DateTime.Now:yyyyMMdd-HHmm}.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _todoBackupService.ExportToFileAsync(dialog.FileName);
            _notificationService.ShowSuccessMessage("待办事项已备份。");
        }
        catch (Exception ex)
        {
            _notificationService.ShowErrorMessage($"备份失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RestoreTodosAsync()
    {
        var confirmed = _notificationService.ShowConfirmDialog(
            "还原将覆盖当前所有待办事项，是否继续？",
            "确认还原");
        if (!confirmed)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "还原待办事项",
            Filter = "JSON 文件 (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var count = await _todoBackupService.ImportFromFileAsync(dialog.FileName);
            await _mainWindowViewModel.ReloadTodosAsync();
            _notificationService.ShowSuccessMessage($"已还原 {count} 条待办事项。");
        }
        catch (Exception ex)
        {
            _notificationService.ShowErrorMessage($"还原失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private void ResetLayout()
    {
        var result = _notificationService.ShowConfirmDialog("确定要恢复默认布局吗？这将重置所有卡片的位置和大小。", "确认恢复");
        if (!result) return;

        try
        {
            _layoutService.ResetToDefault();
            _notificationService.ShowSuccessMessage("布局已重置，重启应用后生效");
        }
        catch (Exception ex)
        {
            _notificationService.ShowErrorMessage($"重置布局失败：{ex.Message}");
        }
    }

    public void RevertChanges()
    {
        try
        {
            IsEditingWeatherApi = false;
            RevertToOriginalSettings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SettingsViewModel.RevertChanges");
        }
    }

    private void RevertToOriginalSettings()
    {
        _isLoading = true;
        try
        {
            if (_originalSettings.TryGetValue("ColorScheme", out var scheme))
            {
                SelectedColorScheme = AppColorSchemeCatalog.NormalizeId(scheme);
            }

            if (_originalSettings.TryGetValue("Startup", out var startup))
            {
                StartupEnabled = bool.Parse(startup);
            }

            if (_originalSettings.TryGetValue("WindowOpacity", out var opacity))
            {
                WindowOpacity = double.Parse(opacity, CultureInfo.InvariantCulture);
            }

            if (_originalSettings.TryGetValue("PanelWidth", out var width))
            {
                PanelWidth = double.Parse(width, CultureInfo.InvariantCulture);
            }

            if (_originalSettings.TryGetValue("WeatherApiKey", out var apiKey))
            {
                WeatherApiKey = apiKey;
            }

            if (_originalSettings.TryGetValue("WeatherApiHost", out var apiHost))
            {
                WeatherApiHost = apiHost;
            }

            if (_originalSettings.TryGetValue("ShortcutMaxCount", out var shortcutMaxCount))
            {
                ShortcutMaxCount = ShortcutLimitHelper.ParseLimit(shortcutMaxCount);
            }
        }
        finally
        {
            _isLoading = false;
        }

        AppColorSchemeCatalog.Apply(SelectedColorScheme);
        ApplyWindowPreview();
        _mainWindowViewModel.SetShortcutLimitPreview(null);
        _ = _mainWindowViewModel.ReloadShortcutsAsync();
    }

    private bool ReadStartupSetting()
    {
        var isEnabled = _startupService.IsEnabled;
        _settingsService.SetValue("Startup", isEnabled.ToString());
        return isEnabled;
    }

    private void ApplyStartupSetting()
    {
        var desired = StartupEnabled;
        var wasEnabled = _startupService.IsEnabled;

        _startupService.SyncWithSetting(desired);

        if (desired && !_startupService.IsEnabled)
        {
            StartupEnabled = false;
            _settingsService.SetValue("Startup", "false");
            _notificationService.ShowWarningMessage("开机自启未能创建计划任务，已恢复为关闭状态。");
        }
        else if (!desired && wasEnabled && _startupService.IsEnabled)
        {
            StartupEnabled = true;
            _settingsService.SetValue("Startup", "true");
            _notificationService.ShowWarningMessage("取消开机自启失败，请稍后重试。");
        }
    }

    public event EventHandler<bool>? RequestClose;
}
