using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniDesk;
using UniDesk.Helpers;
using UniDesk.Models;
using UniDesk.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace UniDesk.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private readonly ILayoutService _layoutService;
    private readonly IClockService _clockService;
    private readonly INoteService _noteService;
    private readonly ITodoService _todoService;
    private readonly IShortcutService _shortcutService;
    private readonly IWeatherService _weatherService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IStartupService _startupService;
    private readonly ITodoBackupService _todoBackupService;
    private readonly ISystemMetricsService _systemMetricsService;
    private readonly DispatcherTimer _weatherRefreshTimer;
    private readonly DispatcherTimer _systemMetricsTimer;
    private CancellationTokenSource? _weatherRefreshCts;
    private int _notesLoadGeneration;
    private int _todosLoadGeneration;
    private int _shortcutsLoadGeneration;
    private int? _shortcutLimitPreview;
    private bool _disposed;

    [ObservableProperty]
    private bool _isTopMost = true;

    /// <summary>窗口已锁定，不可拖动。</summary>
    [ObservableProperty]
    private bool _isWindowLocked;

    /// <summary>面板已收缩，仅显示标题与时间。</summary>
    [ObservableProperty]
    private bool _isPanelCollapsed;

    [ObservableProperty]
    private double _windowOpacity = 0.70;

    [ObservableProperty]
    private double _panelWidth = 320;

    [ObservableProperty]
    private double _panelHeight = 702;

    [ObservableProperty]
    private double _fontScale = 1.0;

    [ObservableProperty]
    private string _displayTitle = "UniDesk";

    [ObservableProperty]
    private bool _isEditingShortcuts;

    [ObservableProperty]
    private string _clockTimeText = "--:--";

    [ObservableProperty]
    private string _clockDateText = "--";

    [ObservableProperty]
    private string _clockLunarText = string.Empty;

    [ObservableProperty]
    private bool _isCalendarPopupOpen;

    [ObservableProperty]
    private string _calendarMonthTitle = string.Empty;

    [ObservableProperty]
    private string _calendarSelectedDetailText = string.Empty;

    public IReadOnlyList<string> CalendarWeekdayLabels => CalendarDayBuilder.WeekdayLabelsList;

    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = new();

    private DateTime _calendarDisplayMonth = DateTime.Today;
    private DateTime _calendarSelectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<NoteItem> _notes = new();

    [ObservableProperty]
    private ObservableCollection<TodoItem> _todos = new();

    [ObservableProperty]
    private ObservableCollection<ShortcutItem> _shortcuts = new();

    /// <summary>快捷方式区展示项（含末尾添加按钮占位）。</summary>
    public ObservableCollection<object> ShortcutDisplayEntries { get; } = new();

    [ObservableProperty]
    private bool _isShortcutAddMenuOpen;

    [ObservableProperty]
    private bool _isSystemAppMenuOpen;

    public IReadOnlyList<SystemAppShortcut> SystemApps => SystemAppCatalog.Apps;

    [ObservableProperty]
    private string _weatherCity = string.Empty;

    [ObservableProperty]
    private string _weatherTemperature = "--";

    [ObservableProperty]
    private string _weatherDescription = string.Empty;

    [ObservableProperty]
    private string _weatherDetailLine = string.Empty;

    [ObservableProperty]
    private string _weatherRangeLine = string.Empty;

    [ObservableProperty]
    private ImageSource? _weatherIconImage;

    [ObservableProperty]
    private bool _useWeatherIconImage;

    [ObservableProperty]
    private string _weatherIconGlyph = string.Empty;

    [ObservableProperty]
    private Brush _weatherIconForeground = Brushes.White;

    [ObservableProperty]
    private string _weatherStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isWeatherLoading;

    [ObservableProperty]
    private bool _hasWeatherData;

    [ObservableProperty]
    private string _systemCpuUsageText = "--";

    [ObservableProperty]
    private string _systemCpuTemperatureText = "CPU --";

    [ObservableProperty]
    private string _systemMemoryUsageText = "--";

    [ObservableProperty]
    private string _systemGpuUsageText = "--";

    [ObservableProperty]
    private string _systemGpuTemperatureText = "GPU --";

    [ObservableProperty]
    private string _systemNetworkReceivedText = "--";

    [ObservableProperty]
    private string _systemNetworkSentText = "--";

    public string WindowLockToolTip => IsWindowLocked ? "打开锁定" : "锁定窗口";

    public string PanelCollapseToolTip => IsPanelCollapsed ? "打开面板" : "收缩面板";

    [ObservableProperty]
    private TodoItem? _collapsedPanelTodo;

    [ObservableProperty]
    private string _collapsedPanelTodoDueText = string.Empty;

    public bool HasCollapsedPanelTodo => CollapsedPanelTodo != null;

    public MainWindowViewModel(
        INotificationService notificationService,
        ISettingsService settingsService,
        IWindowService windowService,
        IHotkeyService hotkeyService,
        ILayoutService layoutService,
        IClockService clockService,
        INoteService noteService,
        ITodoService todoService,
        IShortcutService shortcutService,
        IWeatherService weatherService,
        IStartupService startupService,
        ITodoBackupService todoBackupService,
        ISystemMetricsService systemMetricsService)
    {
        _notificationService = notificationService;
        _settingsService = settingsService;
        _windowService = windowService;
        _layoutService = layoutService;
        _clockService = clockService;
        _noteService = noteService;
        _todoService = todoService;
        _shortcutService = shortcutService;
        _weatherService = weatherService;
        _hotkeyService = hotkeyService;
        _startupService = startupService;
        _todoBackupService = todoBackupService;
        _systemMetricsService = systemMetricsService;

        LoadSettings();
        _layoutService.LoadOrGetDefault();

        _clockService.TimeChanged += ClockService_OnTimeChanged;
        _clockService.Start();
        UpdateClockText();
        RefreshCalendarDays();

        _weatherRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _weatherRefreshTimer.Tick += (_, _) => _ = RefreshWeatherCoreAsync(notifyUser: false);
        _weatherRefreshTimer.Start();

        _systemMetricsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _systemMetricsTimer.Tick += (_, _) => UpdateSystemMetrics();
        _systemMetricsTimer.Start();
        UpdateSystemMetrics();

        _ = LoadNotesAsync();
        _ = LoadTodosAsync();
        _ = LoadShortcutsAsync();
        _ = InitializeWeatherAsync();
    }

    private void LoadSettings()
    {
        IsTopMost = _settingsService.GetSetting("TopMost", true);
        WindowOpacity = _settingsService.GetSetting("WindowOpacity", 0.70);
        IsWindowLocked = _settingsService.GetSetting("WindowLocked", false);
        IsPanelCollapsed = _settingsService.GetSetting("PanelCollapsed", false);
        var savedPanelWidth = _settingsService.GetSetting("PanelWidth", 320.0);
        if (savedPanelWidth < IWindowService.MinPanelWidth) savedPanelWidth = IWindowService.MinPanelWidth;
        if (savedPanelWidth > IWindowService.MaxPanelWidth) savedPanelWidth = IWindowService.MaxPanelWidth;
        PanelWidth = savedPanelWidth;

        var savedPanelHeight = _settingsService.GetSetting("PanelHeight", 702.0);
        if (savedPanelHeight < IWindowService.MinPanelHeight) savedPanelHeight = IWindowService.MinPanelHeight;
        if (savedPanelHeight > IWindowService.MaxPanelHeight) savedPanelHeight = IWindowService.MaxPanelHeight;
        PanelHeight = savedPanelHeight;

        var savedFontScale = _settingsService.GetSetting("FontScale", 1.0);
        if (savedFontScale < 0.9) savedFontScale = 0.9;
        if (savedFontScale > 1.18) savedFontScale = 1.18;
        FontScale = savedFontScale;

        DisplayTitle = NormalizeDisplayTitle(_settingsService.GetValue("DisplayTitle", "UniDesk"));
    }

    public void UpdatePanelWidth(double width)
    {
        if (width < IWindowService.MinPanelWidth) width = IWindowService.MinPanelWidth;
        if (width > IWindowService.MaxPanelWidth) width = IWindowService.MaxPanelWidth;
        PanelWidth = width;
        _settingsService.SetValue("PanelWidth", width.ToString(CultureInfo.InvariantCulture));
        _windowService.SetWidth(width);
    }

    public void UpdatePanelHeight(double height)
    {
        if (height < IWindowService.MinPanelHeight) height = IWindowService.MinPanelHeight;
        if (height > IWindowService.MaxPanelHeight) height = IWindowService.MaxPanelHeight;
        PanelHeight = height;
        _settingsService.SetValue("PanelHeight", height.ToString(CultureInfo.InvariantCulture));
        if (!IsPanelCollapsed)
        {
            _windowService.SetHeight(height);
        }
    }

    public void UpdateFontScale(double scale)
    {
        if (scale < 0.9) scale = 0.9;
        if (scale > 1.18) scale = 1.18;
        FontScale = scale;
        _settingsService.SetValue("FontScale", scale.ToString(CultureInfo.InvariantCulture));
    }

    public void UpdateDisplayTitle(string? title)
    {
        DisplayTitle = NormalizeDisplayTitle(title);
        _settingsService.SetValue("DisplayTitle", DisplayTitle);
    }

    public static string NormalizeDisplayTitle(string? title)
    {
        var normalized = string.IsNullOrWhiteSpace(title) ? "UniDesk" : title.Trim();
        return normalized.Length > 20 ? normalized[..20] : normalized;
    }

    private void ClockService_OnTimeChanged() => UpdateClockText();

    private void UpdateClockText()
    {
        try
        {
            var now = _clockService.CurrentTime;
            ClockTimeText = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            ClockDateText = $"{now:yyyy年M月d日} {ToChineseDayOfWeek(now.DayOfWeek)}";
            ClockLunarText = ToChineseLunarText(now);
            if (_calendarSelectedDate.Date == now.Date)
            {
                CalendarSelectedDetailText = BuildCalendarSelectedDetail(now.Date);
            }
        }
        catch
        {
            ClockTimeText = "--:--";
            ClockDateText = "--";
            ClockLunarText = string.Empty;
        }
    }

    private static string ToChineseDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "星期一",
        DayOfWeek.Tuesday => "星期二",
        DayOfWeek.Wednesday => "星期三",
        DayOfWeek.Thursday => "星期四",
        DayOfWeek.Friday => "星期五",
        DayOfWeek.Saturday => "星期六",
        DayOfWeek.Sunday => "星期日",
        _ => ""
    };

    private static string ToChineseLunarText(DateTime date)
    {
        try
        {
            var calendar = new ChineseLunisolarCalendar();
            var lunarYear = calendar.GetYear(date);
            var lunarMonth = calendar.GetMonth(date);
            var lunarDay = calendar.GetDayOfMonth(date);
            var leapMonth = calendar.GetLeapMonth(lunarYear);
            var isLeapMonth = leapMonth > 0 && lunarMonth == leapMonth;

            if (leapMonth > 0 && lunarMonth >= leapMonth)
            {
                lunarMonth--;
            }

            var sexagenaryYear = calendar.GetSexagenaryYear(date);
            var stem = calendar.GetCelestialStem(sexagenaryYear);
            var branch = calendar.GetTerrestrialBranch(sexagenaryYear);

            var stems = new[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
            var branches = new[] { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
            return $"{stems[stem - 1]}{branches[branch - 1]}年 {(isLeapMonth ? "闰" : "")}{ToChineseLunarMonth(lunarMonth)} {CalendarDayBuilder.ToChineseLunarText(date)}";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ToChineseLunarMonth(int month)
    {
        var months = new[] { "", "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月" };
        return month >= 1 && month < months.Length ? months[month] : string.Empty;
    }

    private static string ToChineseLunarDay(int day)
    {
        var days = new[]
        {
            "", "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
            "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
            "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"
        };
        return day >= 1 && day < days.Length ? days[day] : string.Empty;
    }

    [RelayCommand]
    private void ToggleCalendarPopup()
    {
        if (!IsCalendarPopupOpen)
        {
            var today = _clockService.CurrentTime.Date;
            _calendarSelectedDate = today;
            _calendarDisplayMonth = new DateTime(today.Year, today.Month, 1);
            RefreshCalendarDays();
        }

        IsCalendarPopupOpen = !IsCalendarPopupOpen;
    }

    [RelayCommand]
    private void PreviousCalendarMonth()
    {
        _calendarDisplayMonth = _calendarDisplayMonth.AddMonths(-1);
        RefreshCalendarDays();
    }

    [RelayCommand]
    private void NextCalendarMonth()
    {
        _calendarDisplayMonth = _calendarDisplayMonth.AddMonths(1);
        RefreshCalendarDays();
    }

    [RelayCommand]
    private void BackToToday()
    {
        var today = _clockService.CurrentTime.Date;
        _calendarSelectedDate = today;
        _calendarDisplayMonth = new DateTime(today.Year, today.Month, 1);
        RefreshCalendarDays();
    }

    [RelayCommand]
    private void SelectCalendarDate(CalendarDayItem? item)
    {
        if (item == null)
        {
            return;
        }

        _calendarSelectedDate = item.Date.Date;
        _calendarDisplayMonth = new DateTime(item.Date.Year, item.Date.Month, 1);
        RefreshCalendarDays();
    }

    private void RefreshCalendarDays()
    {
        CalendarMonthTitle = $"{_calendarDisplayMonth:yyyy年M月}";
        CalendarSelectedDetailText = BuildCalendarSelectedDetail(_calendarSelectedDate);
        CalendarDays.Clear();
        foreach (var day in CalendarDayBuilder.BuildMonth(_calendarDisplayMonth, _calendarSelectedDate))
        {
            CalendarDays.Add(day);
        }
    }

    private static string BuildCalendarSelectedDetail(DateTime date)
    {
        var lunarYear = CalendarDayBuilder.ToChineseLunarYearText(date);
        var lunarDay = CalendarDayBuilder.ToChineseLunarText(date);
        return $"{date:yyyy年M月d日} {ToChineseDayOfWeek(date.DayOfWeek)}  {lunarYear} {lunarDay}".Trim();
    }

    [RelayCommand]
    private void ToggleTopMost()
    {
        IsTopMost = !IsTopMost;
        _settingsService.SetValue("TopMost", IsTopMost.ToString());
        _windowService.SetTopMost(IsTopMost);
    }

    [RelayCommand]
    private void ToggleWindowLock() => IsWindowLocked = !IsWindowLocked;

    [RelayCommand]
    private void TogglePanelCollapse() => IsPanelCollapsed = !IsPanelCollapsed;

    partial void OnIsWindowLockedChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowLockToolTip));
        _settingsService.SetValue("WindowLocked", value.ToString());
    }

    partial void OnIsPanelCollapsedChanged(bool value)
    {
        OnPropertyChanged(nameof(PanelCollapseToolTip));
        _settingsService.SetValue("PanelCollapsed", value.ToString());
    }

    public (double Left, double Top)? GetSavedWindowPosition()
    {
        var leftText = _settingsService.GetSetting("WindowLeft");
        var topText = _settingsService.GetSetting("WindowTop");
        if (!double.TryParse(leftText, NumberStyles.Float, CultureInfo.InvariantCulture, out var left) ||
            !double.TryParse(topText, NumberStyles.Float, CultureInfo.InvariantCulture, out var top) ||
            !double.IsFinite(left) ||
            !double.IsFinite(top))
        {
            return null;
        }

        return (left, top);
    }

    public void SaveWindowPosition(double left, double top)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top))
        {
            return;
        }

        _settingsService.SetValue("WindowLeft", left.ToString(CultureInfo.InvariantCulture));
        _settingsService.SetValue("WindowTop", top.ToString(CultureInfo.InvariantCulture));
    }

    partial void OnCollapsedPanelTodoChanged(TodoItem? value) => OnPropertyChanged(nameof(HasCollapsedPanelTodo));

    public Task ReloadTodosAsync() => LoadTodosAsync();

    [RelayCommand]
    private void ToggleWindowVisibility() => _windowService.ToggleWindow();

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsViewModel? viewModel = null;

        try
        {
            var owner = Application.Current.MainWindow;
            var ownerWidth = owner?.ActualWidth ?? PanelWidth;
            if (ownerWidth <= 0)
            {
                ownerWidth = PanelWidth;
            }

            var ownerHeight = owner?.ActualHeight ?? 520;
            if (ownerHeight <= 0)
            {
                ownerHeight = 520;
            }

            viewModel = new SettingsViewModel(
                _settingsService,
                _windowService,
                _notificationService,
                _layoutService,
                _weatherService,
                _startupService,
                _todoBackupService,
                this);

            var settingsWindow = new SettingsWindow(viewModel, ownerWidth, ownerHeight);
            if (owner != null)
            {
                settingsWindow.Owner = owner;
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            settingsWindow.ShowActivated = true;
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainWindowViewModel.OpenSettings");
            _notificationService.ShowErrorMessage($"打开设置失败：{ex.Message}");
            return;
        }

        if (viewModel is not { LastSaveSucceeded: true })
        {
            return;
        }

        var scheme = AppColorSchemeCatalog.NormalizeId(
            _settingsService.GetValue("ColorScheme", _settingsService.GetValue("Theme", AppColorSchemeCatalog.DefaultSchemeId)));
        AppColorSchemeCatalog.Apply(scheme);

        var savedViewModel = viewModel;
        _ = savedViewModel.CompleteSaveFollowUpAsync(
            savedViewModel.PendingApiKey,
            savedViewModel.PendingApiHost,
            savedViewModel.PendingWeatherSettingsChanged);
    }

    public void ApplyWindowSettings()
    {
        LoadSettings();
        _windowService.SetTopMost(IsTopMost);
        _windowService.SetOpacity(WindowOpacity);
        _windowService.SetWidth(PanelWidth);
        if (!IsPanelCollapsed)
        {
            _windowService.SetHeight(PanelHeight);
        }
    }

    public async Task ReloadShortcutsAsync() => await LoadShortcutsAsync();

    [RelayCommand]
    private async Task RefreshNotesAsync() => await LoadNotesAsync();

    [RelayCommand]
    private void NewNote()
    {
        var window = new NoteEditWindow(new NoteEditViewModel(_noteService));
        window.Owner = App.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = LoadNotesAsync();
        }
    }

    [RelayCommand]
    private void EditNote(NoteItem? note)
    {
        if (note == null) return;
        var window = new NoteEditWindow(new NoteEditViewModel(_noteService, note));
        window.Owner = App.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = LoadNotesAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(NoteItem? note)
    {
        if (note == null) return;
        if (!_notificationService.ShowConfirmDialog($"确定删除便签「{note.Title}」？", "删除确认"))
        {
            return;
        }

        await _noteService.DeleteNoteAsync(note.Id);
        await LoadNotesAsync();
    }

    private async Task LoadNotesAsync()
    {
        var generation = Interlocked.Increment(ref _notesLoadGeneration);
        try
        {
            var notes = await _noteService.GetAllNotesAsync();
            if (generation != _notesLoadGeneration) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Notes.Clear();
                foreach (var note in notes.OrderByDescending(n => n.UpdatedAt))
                {
                    Notes.Add(note);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainWindowViewModel.LoadNotes");
            if (generation == _notesLoadGeneration)
            {
                _notificationService.ShowWarningMessage("便签列表加载失败，请稍后重试。");
            }
        }
    }

    [RelayCommand]
    private async Task AddTodoAsync()
    {
        var window = new TodoEditWindow(new TodoEditViewModel(_todoService), PanelWidth);
        window.Owner = App.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            await LoadTodosAsync();
        }
    }

    [RelayCommand]
    private async Task EditTodoAsync(TodoItem? todo)
    {
        if (todo == null) return;

        var window = new TodoEditWindow(new TodoEditViewModel(_todoService, todo), PanelWidth);
        window.Owner = App.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            await LoadTodosAsync();
        }
    }

    [RelayCommand]
    private async Task ToggleTodoAsync(TodoItem? todo)
    {
        if (todo == null) return;
        await _todoService.ToggleCompleteAsync(todo.Id);
        await LoadTodosAsync();
    }

    [RelayCommand]
    private async Task DeleteTodoAsync(TodoItem? todo)
    {
        if (todo == null) return;
        await _todoService.DeleteTodoAsync(todo.Id);
        await LoadTodosAsync();
    }

    private async Task LoadTodosAsync()
    {
        var generation = Interlocked.Increment(ref _todosLoadGeneration);
        try
        {
            var todos = await _todoService.GetAllTodosAsync();
            if (generation != _todosLoadGeneration) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Todos.Clear();
                foreach (var todo in TodoSortHelper.Sort(todos))
                {
                    Todos.Add(todo);
                }

                RefreshCollapsedPanelTodo();
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainWindowViewModel.LoadTodos");
            if (generation == _todosLoadGeneration)
            {
                _notificationService.ShowWarningMessage("待办列表加载失败，请稍后重试。");
            }
        }
    }

    private void RefreshCollapsedPanelTodo()
    {
        CollapsedPanelTodo = Todos.FirstOrDefault(todo => !todo.IsCompleted) ?? Todos.FirstOrDefault();
        CollapsedPanelTodoDueText = BuildTodoDueText(CollapsedPanelTodo);
    }

    private static string BuildTodoDueText(TodoItem? todo)
    {
        if (todo?.DueDate == null)
        {
            return string.Empty;
        }

        var due = todo.DueDate.Value;
        var today = DateTime.Today;
        var hasTime = due.TimeOfDay.TotalSeconds > 0;

        if (due.Date == today)
        {
            return hasTime ? $"今天 {due:HH:mm}" : "今天";
        }

        if (due.Date == today.AddDays(1))
        {
            return hasTime ? $"明天 {due:HH:mm}" : "明天";
        }

        return hasTime
            ? due.ToString("M/d HH:mm", CultureInfo.CurrentCulture)
            : due.ToString("M/d", CultureInfo.CurrentCulture);
    }

    [RelayCommand]
    private async Task LaunchShortcutAsync(ShortcutItem? shortcut)
    {
        if (shortcut == null || IsEditingShortcuts) return;
        await _shortcutService.LaunchShortcutAsync(shortcut.Id);
    }

    [RelayCommand]
    private void ToggleShortcutEdit()
    {
        IsEditingShortcuts = !IsEditingShortcuts;
        if (!IsEditingShortcuts)
        {
            IsShortcutAddMenuOpen = false;
            IsSystemAppMenuOpen = false;
        }
    }

    partial void OnIsEditingShortcutsChanged(bool value)
    {
        if (!value)
        {
            IsShortcutAddMenuOpen = false;
            IsSystemAppMenuOpen = false;
        }

        RefreshShortcutDisplayEntries();
    }

    [RelayCommand]
    private void OpenShortcutAddMenu()
    {
        IsSystemAppMenuOpen = false;
        IsShortcutAddMenuOpen = !IsShortcutAddMenuOpen;
    }

    [RelayCommand]
    private void CloseShortcutAddMenus()
    {
        IsShortcutAddMenuOpen = false;
        IsSystemAppMenuOpen = false;
    }

    [RelayCommand]
    private void OpenSystemAppMenu()
    {
        IsSystemAppMenuOpen = true;
    }

    [RelayCommand]
    private void BackToShortcutAddMenu()
    {
        IsSystemAppMenuOpen = false;
    }

    [RelayCommand]
    private void AddShortcutFromFile()
    {
        CloseShortcutAddMenus();

        var path = ShortcutPickDialogHelper.PickFile();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _ = CreateShortcutAndReloadAsync(ShortcutPathHelper.CreateFromPath(path, Shortcuts.Count));
    }

    [RelayCommand]
    private void AddShortcutFromFolder()
    {
        CloseShortcutAddMenus();

        var folderDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择要添加的文件夹"
        };

        if (folderDialog.ShowDialog() != true)
        {
            return;
        }

        var path = folderDialog.FolderName;
        _ = CreateShortcutAndReloadAsync(new ShortcutItem
        {
            Name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            Path = path,
            Type = ShortcutType.Folder,
            SortOrder = Shortcuts.Count
        });
    }

    [RelayCommand]
    private void AddShortcutFromSystemApp(SystemAppShortcut? app)
    {
        if (app == null)
        {
            return;
        }

        CloseShortcutAddMenus();
        _ = CreateShortcutAndReloadAsync(new ShortcutItem
        {
            Name = app.Name,
            Path = app.Path,
            LaunchArguments = app.LaunchArguments,
            IconLookupPath = app.IconLookupPath ?? app.Path,
            BundledIconFileName = app.BundledIconFileName,
            Type = app.Type,
            SortOrder = Shortcuts.Count
        });
    }

    public void SetShortcutLimitPreview(int? limit)
    {
        _shortcutLimitPreview = limit;
        RefreshShortcutDisplayEntries();
    }

    private int GetShortcutMaxCount() =>
        _shortcutLimitPreview
        ?? ShortcutLimitHelper.ParseLimit(_settingsService.GetValue("ShortcutMaxCount", ShortcutLimitHelper.DefaultLimit.ToString()));

    private static bool IsDuplicateShortcut(IEnumerable<ShortcutItem> existing, ShortcutItem candidate)
    {
        return existing.Any(s =>
            string.Equals(s.Path, candidate.Path, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.LaunchArguments ?? string.Empty, candidate.LaunchArguments ?? string.Empty, StringComparison.Ordinal));
    }

    private async Task CreateShortcutAndReloadAsync(ShortcutItem shortcut)
    {
        var maxCount = GetShortcutMaxCount();
        var allShortcuts = await _shortcutService.GetAllShortcutsAsync();

        if (allShortcuts.Count >= maxCount)
        {
            _notificationService.ShowWarningMessage($"最多只能添加 {maxCount} 个快捷方式。");
            return;
        }

        if (IsDuplicateShortcut(allShortcuts, shortcut))
        {
            _notificationService.ShowWarningMessage("该快捷方式已添加。");
            return;
        }

        var id = await _shortcutService.CreateShortcutAsync(shortcut);
        if (id <= 0)
        {
            _notificationService.ShowWarningMessage("添加快捷方式失败，请稍后重试。");
            return;
        }

        await LoadShortcutsAsync();
    }

    private void RefreshShortcutDisplayEntries()
    {
        ShortcutDisplayEntries.Clear();
        foreach (var shortcut in Shortcuts)
        {
            ShortcutDisplayEntries.Add(shortcut);
        }

        if (IsEditingShortcuts && Shortcuts.Count < GetShortcutMaxCount())
        {
            ShortcutDisplayEntries.Add(AddShortcutPlaceholder.Instance);
        }
    }

    [RelayCommand]
    private async Task DeleteShortcutAsync(ShortcutItem? shortcut)
    {
        if (shortcut == null) return;

        await _shortcutService.DeleteShortcutAsync(shortcut.Id);
        await LoadShortcutsAsync();
    }

    public async Task MoveShortcutAsync(ShortcutItem? source, ShortcutItem? target)
    {
        if (source == null || target == null || source.Id == target.Id)
        {
            return;
        }

        var sourceIndex = Shortcuts.IndexOf(source);
        var targetIndex = Shortcuts.IndexOf(target);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        Shortcuts.Move(sourceIndex, targetIndex);

        var sourceDisplayIndex = ShortcutDisplayEntries.IndexOf(source);
        var targetDisplayIndex = ShortcutDisplayEntries.IndexOf(target);
        if (sourceDisplayIndex >= 0 && targetDisplayIndex >= 0)
        {
            ShortcutDisplayEntries.Move(sourceDisplayIndex, targetDisplayIndex);
        }

        await _shortcutService.UpdateSortOrderAsync(Shortcuts.Select(s => s.Id).ToList());
    }

    private async Task LoadShortcutsAsync()
    {
        var generation = Interlocked.Increment(ref _shortcutsLoadGeneration);
        try
        {
            var shortcuts = await _shortcutService.GetAllShortcutsAsync();
            if (generation != _shortcutsLoadGeneration) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Shortcuts.Clear();
                foreach (var shortcut in shortcuts.OrderBy(s => s.SortOrder).Take(GetShortcutMaxCount()))
                {
                    Shortcuts.Add(shortcut);
                }

                RefreshShortcutDisplayEntries();
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainWindowViewModel.LoadShortcuts");
            if (generation == _shortcutsLoadGeneration)
            {
                _notificationService.ShowWarningMessage("快捷方式列表加载失败，请稍后重试。");
            }
        }
    }

    private async Task InitializeWeatherAsync()
    {
        ApplyWeatherInfo(await _weatherService.GetCachedWeatherAsync());

        if (string.IsNullOrEmpty(_weatherService.GetEffectiveApiKey()))
        {
            WeatherStatusMessage = "请在设置中配置 API Key";
            return;
        }

        await RefreshWeatherCoreAsync(notifyUser: false);
    }

    public async Task RefreshWeatherAfterSettingsAsync()
    {
        await RefreshWeatherCoreAsync(notifyUser: false);
    }

    [RelayCommand]
    private async Task RefreshWeatherAsync() => await RefreshWeatherCoreAsync(notifyUser: true);

    private async Task RefreshWeatherCoreAsync(bool notifyUser)
    {
        _weatherRefreshCts?.Cancel();
        _weatherRefreshCts?.Dispose();
        _weatherRefreshCts = new CancellationTokenSource();

        if (string.IsNullOrEmpty(_weatherService.GetEffectiveApiKey()))
        {
            ApplyWeatherInfo(await _weatherService.GetCachedWeatherAsync());
            WeatherStatusMessage = "请在设置中配置 API Key";
            return;
        }

        IsWeatherLoading = true;
        WeatherStatusMessage = string.Empty;

        try
        {
            var info = await _weatherService.RefreshWeatherAsync(_weatherRefreshCts.Token, notifyUser);
            ApplyWeatherInfo(info ?? await _weatherService.GetCachedWeatherAsync());
        }
        catch (OperationCanceledException)
        {
            ApplyWeatherInfo(await _weatherService.GetCachedWeatherAsync());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainWindowViewModel.RefreshWeather");
            ApplyWeatherInfo(await _weatherService.GetCachedWeatherAsync());
            if (notifyUser)
            {
                _notificationService.ShowWarningMessage("天气刷新失败，请检查网络与 API 配置。");
            }
        }
        finally
        {
            IsWeatherLoading = false;
        }
    }

    private void ApplyWeatherInfo(WeatherInfo? info)
    {
        if (info == null)
        {
            HasWeatherData = false;
            WeatherCity = "天气加载失败，请检查网络或 API 配置";
            WeatherTemperature = "--";
            WeatherDescription = string.Empty;
            WeatherDetailLine = string.Empty;
            WeatherRangeLine = string.Empty;
            WeatherIconImage = null;
            UseWeatherIconImage = false;
            WeatherIconGlyph = string.Empty;
            WeatherIconForeground = Brushes.White;
            if (string.IsNullOrEmpty(WeatherStatusMessage))
            {
                WeatherStatusMessage = string.Empty;
            }

            return;
        }

        HasWeatherData = true;
        WeatherCity = info.City;
        WeatherTemperature = info.Temperature;
        WeatherDescription = info.WeatherDesc;
        ApplyWeatherIcon(info);

        var details = new List<string>
        {
            string.IsNullOrWhiteSpace(info.AirQuality) ? "空气 --" : info.AirQuality
        };

        if (!string.IsNullOrWhiteSpace(info.Humidity))
        {
            details.Add(info.Humidity);
        }

        var range = BuildTempRange(info.MinTemp, info.MaxTemp);
        WeatherDetailLine = string.Join("  |  ", details);
        WeatherRangeLine = range;
        WeatherStatusMessage = info.IsExpired ? "数据可能已过期" : string.Empty;
    }

    private static string BuildTempRange(string minTemp, string maxTemp)
    {
        if (string.IsNullOrWhiteSpace(minTemp) && string.IsNullOrWhiteSpace(maxTemp))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(minTemp)) return maxTemp;
        if (string.IsNullOrWhiteSpace(maxTemp)) return minTemp;
        return $"{minTemp} ~ {maxTemp}";
    }

    private void ApplyWeatherIcon(WeatherInfo info)
    {
        var iconCode = ResolveIconCode(info);
        var display = WeatherIconResolver.Resolve(iconCode, info.WeatherDesc);
        UseWeatherIconImage = display.UseImage;
        WeatherIconImage = display.ImageSource;
        WeatherIconGlyph = display.Glyph;
        WeatherIconForeground = display.GlyphForeground;
    }

    private static string ResolveIconCode(WeatherInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.IconCode))
        {
            return info.IconCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(info.IconUri))
        {
            var fileName = Path.GetFileNameWithoutExtension(info.IconUri);
            if (!string.IsNullOrEmpty(fileName) && fileName.All(char.IsDigit))
            {
                return fileName;
            }
        }

        return WeatherIconResolver.NormalizeIconCode(null, info.WeatherDesc);
    }

    private void UpdateSystemMetrics()
    {
        try
        {
            var metrics = _systemMetricsService.Read();
            SystemCpuUsageText = FormatPercent(metrics.CpuUsage);
            SystemCpuTemperatureText = FormatTemperature("CPU", metrics.CpuTemperature);
            SystemMemoryUsageText = FormatPercent(metrics.MemoryUsage);
            SystemGpuUsageText = FormatPercent(metrics.GpuUsage);
            SystemGpuTemperatureText = FormatTemperature("GPU", metrics.GpuTemperature);
            SystemNetworkReceivedText = FormatSpeed(metrics.NetworkReceivedBytesPerSecond);
            SystemNetworkSentText = FormatSpeed(metrics.NetworkSentBytesPerSecond);
        }
        catch
        {
            SystemCpuUsageText = "--";
            SystemCpuTemperatureText = "CPU --";
            SystemMemoryUsageText = "--";
            SystemGpuUsageText = "--";
            SystemGpuTemperatureText = "GPU --";
            SystemNetworkReceivedText = "--";
            SystemNetworkSentText = "--";
        }
    }

    private static string FormatPercent(double? value) => value.HasValue ? $"{value.Value:0}%" : "--";

    private static string FormatTemperature(string label, double? value) =>
        value.HasValue ? $"{label} {value.Value:0}℃" : $"{label} --";

    private static string FormatSpeed(double? bytesPerSecond)
    {
        if (!bytesPerSecond.HasValue) return "--";

        var value = Math.Max(0, bytesPerSecond.Value);
        if (value < 1) return "0 B/s";
        if (value < 1024) return $"{value:0} B/s";

        value /= 1024;
        if (value < 1024) return $"{value:0} KB/s";

        value /= 1024;
        if (value < 1024) return $"{value:0.0} MB/s";

        value /= 1024;
        return $"{value:0.0} GB/s";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _weatherRefreshTimer.Stop();
        _systemMetricsTimer.Stop();
        var weatherRefreshCts = _weatherRefreshCts;
        _weatherRefreshCts = null;
        try
        {
            weatherRefreshCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        weatherRefreshCts?.Dispose();
        _clockService.Stop();
        if (_systemMetricsService is IDisposable disposableMetrics)
        {
            disposableMetrics.Dispose();
        }
    }
}
