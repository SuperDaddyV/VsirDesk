using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniDesk.Helpers;
using UniDesk.Models;
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
    private readonly IQuickTextService _quickTextService;
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
    private double _panelHeight;

    [ObservableProperty]
    private double _fontScale = 1.0;

    [ObservableProperty]
    private string _displayTitle = "UniDesk";

    [ObservableProperty]
    private string _weatherApiKey = string.Empty;

    [ObservableProperty]
    private string _weatherApiHost = string.Empty;

    [ObservableProperty]
    private bool _isEditingWeatherApi;

    [ObservableProperty]
    private int _shortcutMaxCount = ShortcutLimitHelper.DefaultLimit;

    [ObservableProperty]
    private bool _clipboardHistoryEnabled = true;

    [ObservableProperty]
    private bool _clipboardSensitiveFilterEnabled = true;

    [ObservableProperty]
    private int _clipboardHistoryMaxCount = QuickTextService.DefaultHistoryLimit;

    public IReadOnlyList<int> ClipboardHistoryLimitOptions => QuickTextService.AllowedHistoryLimits;

    public ObservableCollection<ModuleSettingOptionViewModel> ModuleSettings { get; } = new();

    private List<ModuleSetting> _originalModuleSettings = [];

    public string FontScaleLabel => FontScale switch
    {
        <= 0.95 => "小",
        >= 1.1 => "大",
        _ => "标准"
    };

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
        IQuickTextService quickTextService,
        MainWindowViewModel mainWindowViewModel)
    {
        _settingsService = settingsService;
        _windowService = windowService;
        _notificationService = notificationService;
        _layoutService = layoutService;
        _weatherService = weatherService;
        _startupService = startupService;
        _todoBackupService = todoBackupService;
        _quickTextService = quickTextService;
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
            PanelHeight = _settingsService.GetSetting("PanelHeight", 702.0);
            FontScale = _settingsService.GetSetting("FontScale", 1.0);
            DisplayTitle = MainWindowViewModel.NormalizeDisplayTitle(_settingsService.GetValue("DisplayTitle", "UniDesk"));
            WeatherApiKey = _settingsService.GetValue("WeatherApiKey", "");
            WeatherApiHost = _settingsService.GetValue("WeatherApiHost", "");
            ShortcutMaxCount = ShortcutLimitHelper.ParseLimit(
                _settingsService.GetValue("ShortcutMaxCount", ShortcutLimitHelper.DefaultLimit.ToString()));
            ClipboardHistoryEnabled = _settingsService.GetSetting(QuickTextService.HistoryEnabledSettingKey, true);
            ClipboardSensitiveFilterEnabled = _settingsService.GetSetting(QuickTextService.SensitiveFilterSettingKey, true);
            ClipboardHistoryMaxCount = QuickTextService.NormalizeHistoryLimit(
                _settingsService.GetSetting(QuickTextService.HistoryMaxCountSettingKey, QuickTextService.DefaultHistoryLimit));
            LoadModuleSettings(_mainWindowViewModel.GetModuleSettingsSnapshot());

            PanelWidth = Math.Clamp(PanelWidth, IWindowService.MinPanelWidth, IWindowService.MaxPanelWidth);
            PanelHeight = Math.Clamp(PanelHeight, IWindowService.MinPanelHeight, IWindowService.MaxPanelHeight);
            FontScale = Math.Clamp(FontScale, 0.9, 1.18);
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
        _originalSettings["PanelHeight"] = PanelHeight.ToString(CultureInfo.InvariantCulture);
        _originalSettings["FontScale"] = FontScale.ToString(CultureInfo.InvariantCulture);
        _originalSettings["DisplayTitle"] = DisplayTitle;
        _originalSettings["WeatherApiKey"] = WeatherApiKey;
        _originalSettings["WeatherApiHost"] = WeatherApiHost;
        _originalSettings["ShortcutMaxCount"] = ShortcutMaxCount.ToString(CultureInfo.InvariantCulture);
        _originalSettings["ClipboardHistoryEnabled"] = ClipboardHistoryEnabled.ToString();
        _originalSettings["ClipboardSensitiveFilterEnabled"] = ClipboardSensitiveFilterEnabled.ToString();
        _originalSettings["ClipboardHistoryMaxCount"] = ClipboardHistoryMaxCount.ToString(CultureInfo.InvariantCulture);
        _originalModuleSettings = ModuleSettings.Select(module => module.ToModel().Clone()).ToList();
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

    [RelayCommand]
    private void SelectClipboardHistoryLimit(string? limitText)
    {
        if (!int.TryParse(limitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit))
        {
            return;
        }

        ClipboardHistoryMaxCount = QuickTextService.NormalizeHistoryLimit(limit);
    }

    [RelayCommand]
    private async Task ClearClipboardHistoryFromSettingsAsync()
    {
        if (!_notificationService.ShowConfirmDialog("确定清空全部剪贴板历史？", "确认清空"))
        {
            return;
        }

        await _quickTextService.ClearClipboardHistoryAsync();
        await _mainWindowViewModel.ReloadQuickTextAsync();
        _notificationService.ShowSuccessMessage("剪贴板历史已清空。");
    }

    [RelayCommand]
    private void MoveModuleUp(ModuleSettingOptionViewModel? module)
    {
        if (module == null)
        {
            return;
        }

        var index = ModuleSettings.IndexOf(module);
        if (index <= 0)
        {
            return;
        }

        ModuleSettings.Move(index, index - 1);
        RefreshModuleSortState();
        ApplyModulePreview();
    }

    [RelayCommand]
    private void MoveModuleDown(ModuleSettingOptionViewModel? module)
    {
        if (module == null)
        {
            return;
        }

        var index = ModuleSettings.IndexOf(module);
        if (index < 0 || index >= ModuleSettings.Count - 1)
        {
            return;
        }

        ModuleSettings.Move(index, index + 1);
        RefreshModuleSortState();
        ApplyModulePreview();
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

    partial void OnPanelHeightChanged(double value) => ApplyWindowPreview();

    partial void OnFontScaleChanged(double value)
    {
        OnPropertyChanged(nameof(FontScaleLabel));
        ApplyWindowPreview();
    }

    partial void OnDisplayTitleChanged(string value) => ApplyWindowPreview();

    partial void OnShortcutMaxCountChanged(int value) =>
        _mainWindowViewModel.SetShortcutLimitPreview(value);

    private void LoadModuleSettings(IEnumerable<ModuleSetting> modules)
    {
        foreach (var module in ModuleSettings)
        {
            module.PropertyChanged -= ModuleSetting_OnPropertyChanged;
        }

        ModuleSettings.Clear();
        foreach (var module in DashboardModuleCatalog.Normalize(modules))
        {
            var option = ModuleSettingOptionViewModel.FromModel(module);
            option.PropertyChanged += ModuleSetting_OnPropertyChanged;
            ModuleSettings.Add(option);
        }

        RefreshModuleSortState();
    }

    private void ModuleSetting_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading || e.PropertyName != nameof(ModuleSettingOptionViewModel.IsEnabled))
        {
            return;
        }

        ApplyModulePreview();
    }

    private void RefreshModuleSortState()
    {
        for (var i = 0; i < ModuleSettings.Count; i++)
        {
            ModuleSettings[i].SortOrder = i;
            ModuleSettings[i].CanMoveUp = i > 0;
            ModuleSettings[i].CanMoveDown = i < ModuleSettings.Count - 1;
        }
    }

    private List<ModuleSetting> BuildModuleSettings()
    {
        RefreshModuleSortState();
        return DashboardModuleCatalog.Normalize(ModuleSettings.Select(module => module.ToModel()));
    }

    private void ApplyModulePreview()
    {
        if (_isLoading)
        {
            return;
        }

        _mainWindowViewModel.ApplyModuleSettings(BuildModuleSettings(), persist: false);
    }

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
        if (!_mainWindowViewModel.IsPanelCollapsed)
        {
            _windowService.SetHeight(PanelHeight);
        }

        _mainWindowViewModel.WindowOpacity = WindowOpacity;
        _mainWindowViewModel.PanelWidth = PanelWidth;
        _mainWindowViewModel.PanelHeight = PanelHeight;
        _mainWindowViewModel.FontScale = FontScale;
        _mainWindowViewModel.DisplayTitle = MainWindowViewModel.NormalizeDisplayTitle(DisplayTitle);
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
            DisplayTitle = MainWindowViewModel.NormalizeDisplayTitle(DisplayTitle);
            weatherSettingsChanged =
                _originalSettings.GetValueOrDefault("WeatherApiKey") != apiKeyToValidate ||
                _originalSettings.GetValueOrDefault("WeatherApiHost") != apiHostToValidate;

            _settingsService.SetValue("ColorScheme", SelectedColorScheme);
            _settingsService.SetValue("Theme", SelectedColorScheme);
            _settingsService.SetValue("Startup", StartupEnabled.ToString());
            _settingsService.SetValue("WindowOpacity", WindowOpacity.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue("PanelWidth", PanelWidth.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue("PanelHeight", PanelHeight.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue("FontScale", FontScale.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue("DisplayTitle", MainWindowViewModel.NormalizeDisplayTitle(DisplayTitle));
            _settingsService.SetValue("WeatherApiKey", apiKeyToValidate);
            _settingsService.SetValue("WeatherApiHost", apiHostToValidate);
            _settingsService.SetValue("ShortcutMaxCount", ShortcutMaxCount.ToString(CultureInfo.InvariantCulture));
            _settingsService.SetValue(QuickTextService.HistoryEnabledSettingKey, ClipboardHistoryEnabled.ToString());
            _settingsService.SetValue(QuickTextService.SensitiveFilterSettingKey, ClipboardSensitiveFilterEnabled.ToString());
            _settingsService.SetValue(QuickTextService.HistoryMaxCountSettingKey, ClipboardHistoryMaxCount.ToString(CultureInfo.InvariantCulture));
            _mainWindowViewModel.ApplyModuleSettings(BuildModuleSettings(), persist: true);

            await _settingsService.FlushPendingSavesAsync();
            await _quickTextService.TrimClipboardHistoryAsync(ClipboardHistoryMaxCount);

            AppColorSchemeCatalog.Apply(SelectedColorScheme);
            ApplyWindowPreview();
            _mainWindowViewModel.SetShortcutLimitPreview(null);
            await _mainWindowViewModel.ReloadShortcutsAsync();
            await _mainWindowViewModel.ReloadQuickTextAsync();
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
        PanelHeight = 702;
        FontScale = 1.0;
        DisplayTitle = "UniDesk";
        WeatherApiKey = "";
        WeatherApiHost = "";
        IsEditingWeatherApi = false;
        ShortcutMaxCount = ShortcutLimitHelper.DefaultLimit;
        ClipboardHistoryEnabled = true;
        ClipboardSensitiveFilterEnabled = true;
        ClipboardHistoryMaxCount = QuickTextService.DefaultHistoryLimit;
        LoadModuleSettings(DashboardModuleCatalog.CreateDefaultModules());
        ApplyModulePreview();

        _notificationService.ShowInfoMessage("已恢复默认设置，点击保存后生效");
    }

    [RelayCommand]
    private async Task BackupTodosAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "备份 UniDesk 数据",
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = $"UniDesk-data-{DateTime.Now:yyyyMMdd-HHmm}.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _todoBackupService.ExportToFileAsync(dialog.FileName);
            _notificationService.ShowSuccessMessage("UniDesk 数据已备份。");
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
            "还原将覆盖备份文件中包含的待办事项、快速便签和快捷文本，是否继续？",
            "确认还原");
        if (!confirmed)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "还原 UniDesk 数据",
            Filter = "JSON 文件 (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var result = await _todoBackupService.ImportFromFileAsync(dialog.FileName);
            await _mainWindowViewModel.ReloadTodosAsync();
            await _mainWindowViewModel.ReloadQuickNotesAsync();
            await _mainWindowViewModel.ReloadQuickTextAsync();
            _notificationService.ShowSuccessMessage(
                $"已还原 {result.TodoCount} 条待办事项，{result.QuickNoteCount} 条便签，{result.ClipboardHistoryCount} 条历史，{result.TextSnippetCount} 条常用短语。");
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

            if (_originalSettings.TryGetValue("PanelHeight", out var height))
            {
                PanelHeight = double.Parse(height, CultureInfo.InvariantCulture);
            }

            if (_originalSettings.TryGetValue("FontScale", out var fontScale))
            {
                FontScale = double.Parse(fontScale, CultureInfo.InvariantCulture);
            }

            if (_originalSettings.TryGetValue("DisplayTitle", out var displayTitle))
            {
                DisplayTitle = MainWindowViewModel.NormalizeDisplayTitle(displayTitle);
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

            if (_originalSettings.TryGetValue("ClipboardHistoryEnabled", out var clipboardHistoryEnabled))
            {
                ClipboardHistoryEnabled = bool.Parse(clipboardHistoryEnabled);
            }

            if (_originalSettings.TryGetValue("ClipboardSensitiveFilterEnabled", out var clipboardSensitiveFilterEnabled))
            {
                ClipboardSensitiveFilterEnabled = bool.Parse(clipboardSensitiveFilterEnabled);
            }

            if (_originalSettings.TryGetValue("ClipboardHistoryMaxCount", out var clipboardHistoryMaxCount))
            {
                ClipboardHistoryMaxCount = QuickTextService.NormalizeHistoryLimit(
                    int.Parse(clipboardHistoryMaxCount, CultureInfo.InvariantCulture));
            }

            LoadModuleSettings(_originalModuleSettings);
        }
        finally
        {
            _isLoading = false;
        }

        AppColorSchemeCatalog.Apply(SelectedColorScheme);
        ApplyWindowPreview();
        _mainWindowViewModel.SetShortcutLimitPreview(null);
        _ = _mainWindowViewModel.ReloadShortcutsAsync();
        _mainWindowViewModel.ApplyModuleSettings(_originalModuleSettings, persist: false);
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
            _notificationService.ShowWarningMessage("开机自启设置失败，已恢复为关闭状态。");
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
