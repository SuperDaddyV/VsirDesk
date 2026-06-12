using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UniDesk.Helpers;
using UniDesk.Models;
using UniDesk.Services;
using UniDesk.ViewModels;

namespace UniDesk;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private Point _shortcutDragStart;
    private ShortcutItem? _draggedShortcut;
    private Point _scrollPanStart;
    private double _scrollPanOffsetStart;
    private bool _scrollPanPending;
    private bool _scrollPanActive;
    private bool _suppressPositionSave;
    private const double DefaultExpandedPanelHeight = 702;
    private const double CollapsedPanelHeight = 196;
    private const double WindowCornerRadius = 16;

    public bool AllowShutdown { get; set; }

    public MainWindow(MainWindowViewModel viewModel, IWindowService windowService, ISettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _settingsService = settingsService;
        _ = windowService;

        AppIconHelper.ApplyWindowIcon(this);
        DesktopWidgetWindowHelper.Configure(this);

        ApplyInitialWindowBounds();
        _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
    }

    private void ViewModel_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsPanelCollapsed))
        {
            ApplyPanelCollapseState();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.PanelHeight) && !_viewModel.IsPanelCollapsed)
        {
            ApplyPanelCollapseState();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.ModuleLayoutVersion))
        {
            ApplyModuleLayout();
        }
    }

    private void ApplyPanelCollapseState()
    {
        var targetHeight = _viewModel.IsPanelCollapsed
            ? CollapsedPanelHeight
            : Math.Clamp(_viewModel.PanelHeight, IWindowService.MinPanelHeight, IWindowService.MaxPanelHeight);
        MinHeight = _viewModel.IsPanelCollapsed ? CollapsedPanelHeight : IWindowService.MinPanelHeight;
        MaxHeight = _viewModel.IsPanelCollapsed ? CollapsedPanelHeight : IWindowService.MaxPanelHeight;
        Height = targetHeight;
        ClampToVisibleWorkArea();
        if (MainModulesGrid.RowDefinitions.Count < 4)
        {
            return;
        }

        if (_viewModel.IsPanelCollapsed)
        {
            MainModulesGrid.Margin = new Thickness(12, 0, 12, 6);
            MainModulesGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            MainModulesGrid.RowDefinitions[1].Height = new GridLength(0);
            MainModulesGrid.RowDefinitions[2].Height = new GridLength(0);
            MainModulesGrid.RowDefinitions[3].Height = new GridLength(0);
        }
        else
        {
            MainModulesGrid.Margin = new Thickness(12, 0, 12, 10);
            MainModulesGrid.RowDefinitions[0].Height = new GridLength(24, GridUnitType.Star);
            MainModulesGrid.RowDefinitions[1].Height = new GridLength(16, GridUnitType.Star);
            MainModulesGrid.RowDefinitions[2].Height = new GridLength(26, GridUnitType.Star);
            MainModulesGrid.RowDefinitions[3].Height = new GridLength(34, GridUnitType.Star);
        }

        ApplyModuleLayout();

        if (!_suppressPositionSave)
        {
            SaveWindowPosition();
        }
    }

    private void ApplyInitialWindowBounds()
    {
        _suppressPositionSave = true;
        try
        {
            Height = _viewModel.IsPanelCollapsed
                ? CollapsedPanelHeight
                : Math.Clamp(_viewModel.PanelHeight <= 0 ? DefaultExpandedPanelHeight : _viewModel.PanelHeight,
                    IWindowService.MinPanelHeight,
                    IWindowService.MaxPanelHeight);
            Width = _viewModel.PanelWidth;
            ApplyPanelCollapseState();

            var savedPosition = _viewModel.GetSavedWindowPosition();
            if (savedPosition is { } position)
            {
                Left = position.Left;
                Top = position.Top;
            }
            else
            {
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Right - Width - 20;
                Top = workArea.Top + (workArea.Height - Height) / 2;
            }

            ClampToVisibleWorkArea();
        }
        finally
        {
            _suppressPositionSave = false;
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateWindowContainerClip();
        _viewModel.ApplyWindowSettings();
        ApplyModuleLayout();
    }

    private void ApplyModuleLayout()
    {
        if (MainModulesGrid.RowDefinitions.Count < 4)
        {
            return;
        }

        var modules = _viewModel.GetModuleSettingsSnapshot()
            .Where(module => module.IsEnabled)
            .Select(module => new
            {
                module.ModuleId,
                Element = GetModuleElement(module.ModuleId),
                Weight = GetModuleWeight(module.ModuleId)
            })
            .Where(module => module.Element != null)
            .ToList();

        var visibleModules = _viewModel.IsPanelCollapsed
            ? modules.Take(1).ToList()
            : modules;

        foreach (var element in GetAllModuleElements())
        {
            element.Visibility = Visibility.Collapsed;
        }

        for (var row = 0; row < MainModulesGrid.RowDefinitions.Count; row++)
        {
            MainModulesGrid.RowDefinitions[row].Height = new GridLength(0);
        }

        for (var row = 0; row < visibleModules.Count; row++)
        {
            var module = visibleModules[row];
            var element = module.Element!;
            Grid.SetRow(element, row);
            element.Visibility = Visibility.Visible;
            element.Margin = row == visibleModules.Count - 1
                ? new Thickness(0)
                : new Thickness(0, 0, 0, 6);

            MainModulesGrid.RowDefinitions[row].Height = _viewModel.IsPanelCollapsed
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(module.Weight, GridUnitType.Star);
        }

        EmptyModulesMessage.Visibility = modules.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (modules.Count == 0)
        {
            MainModulesGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
        }
    }

    private FrameworkElement? GetModuleElement(string moduleId) => moduleId switch
    {
        DashboardModuleIds.TimeWeather => TimeWeatherModule,
        DashboardModuleIds.HardwareMonitor => HardwareMonitorModule,
        DashboardModuleIds.Shortcuts => ShortcutsModule,
        DashboardModuleIds.Todos => TodosModule,
        _ => null
    };

    private IEnumerable<FrameworkElement> GetAllModuleElements()
    {
        yield return TimeWeatherModule;
        yield return HardwareMonitorModule;
        yield return ShortcutsModule;
        yield return TodosModule;
    }

    private static double GetModuleWeight(string moduleId) => moduleId switch
    {
        DashboardModuleIds.TimeWeather => 24,
        DashboardModuleIds.HardwareMonitor => 16,
        DashboardModuleIds.Shortcuts => 26,
        DashboardModuleIds.Todos => 34,
        _ => 20
    };

    private void WindowContainer_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateWindowContainerClip();

    private void UpdateWindowContainerClip()
    {
        if (WindowContainer.ActualWidth <= 0 || WindowContainer.ActualHeight <= 0)
        {
            return;
        }

        WindowContainer.Clip = new RectangleGeometry(
            new Rect(0, 0, WindowContainer.ActualWidth, WindowContainer.ActualHeight),
            WindowCornerRadius,
            WindowCornerRadius);
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsWindowLocked ||
            e.LeftButton != MouseButtonState.Pressed ||
            IsInside<Button>(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
        SaveWindowPosition();
        e.Handled = true;
    }

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _viewModel.OpenSettingsCommand.Execute(null);
    }

    private void WindowDragSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsWindowLocked ||
            e.LeftButton != MouseButtonState.Pressed ||
            e.GetPosition(this).Y > 34 ||
            IsInside<Button>(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
        SaveWindowPosition();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e) => Hide();

    private void ClockHotspot_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _viewModel.ToggleCalendarPopupCommand.Execute(null);
        e.Handled = true;
    }

    private void PreviousCalendarMonthButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.PreviousCalendarMonthCommand.Execute(null);
        e.Handled = true;
    }

    private void NextCalendarMonthButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.NextCalendarMonthCommand.Execute(null);
        e.Handled = true;
    }

    private void CloseCalendarButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.IsCalendarPopupOpen = false;
        e.Handled = true;
    }

    private void CalendarDayButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectCalendarDateCommand.Execute((sender as FrameworkElement)?.DataContext as CalendarDayItem);
        e.Handled = true;
    }

    private void OnScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private void ScrollViewer_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer viewer || ShouldIgnoreScrollPan(e.OriginalSource as DependencyObject))
        {
            return;
        }

        _scrollPanPending = true;
        _scrollPanActive = false;
        _scrollPanStart = e.GetPosition(viewer);
        _scrollPanOffsetStart = viewer.VerticalOffset;
    }

    private void ScrollViewer_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (_scrollPanPending)
        {
            var current = e.GetPosition(viewer);
            var deltaX = current.X - _scrollPanStart.X;
            var deltaY = current.Y - _scrollPanStart.Y;

            if (Math.Abs(deltaX) < 4 && Math.Abs(deltaY) < 4)
            {
                return;
            }

            if (Math.Abs(deltaY) <= Math.Abs(deltaX))
            {
                _scrollPanPending = false;
                return;
            }

            _scrollPanPending = false;
            _scrollPanActive = true;
            viewer.CaptureMouse();
            viewer.Cursor = Cursors.SizeAll;
        }

        if (!_scrollPanActive)
        {
            return;
        }

        var position = e.GetPosition(viewer);
        var offset = _scrollPanOffsetStart - (position.Y - _scrollPanStart.Y);
        viewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(viewer.ScrollableHeight, offset)));
        e.Handled = true;
    }

    private void ScrollViewer_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndScrollPan(sender as ScrollViewer);
    }

    private void ScrollViewer_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_scrollPanActive && e.LeftButton != MouseButtonState.Pressed)
        {
            EndScrollPan(sender as ScrollViewer);
        }
    }

    private void EndScrollPan(ScrollViewer? viewer)
    {
        if (!_scrollPanPending && !_scrollPanActive)
        {
            return;
        }

        _scrollPanPending = false;
        _scrollPanActive = false;
        viewer?.ReleaseMouseCapture();
        if (viewer != null)
        {
            viewer.Cursor = null;
        }
    }

    private static bool ShouldIgnoreScrollPan(DependencyObject? source)
    {
        var current = source;
        while (current != null)
        {
            if (current is FrameworkElement { Tag: "TodoCheck" } or Button)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void ShortcutItem_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_viewModel.IsEditingShortcuts ||
            IsInside<Button>(e.OriginalSource as DependencyObject))
        {
            return;
        }

        _shortcutDragStart = e.GetPosition(null);
        _draggedShortcut = (sender as FrameworkElement)?.DataContext as ShortcutItem;
        if (sender is UIElement element)
        {
            element.CaptureMouse();
        }
    }

    private void ShortcutItem_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element && element.IsMouseCaptured)
        {
            element.ReleaseMouseCapture();
        }

        _draggedShortcut = null;
    }

    private void ShortcutItem_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_viewModel.IsEditingShortcuts ||
            e.LeftButton != MouseButtonState.Pressed ||
            _draggedShortcut == null)
        {
            return;
        }

        var current = e.GetPosition(null);
        if (Math.Abs(current.X - _shortcutDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _shortcutDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject(typeof(ShortcutItem), _draggedShortcut);
        _draggedShortcut = null;
        if (sender is UIElement element && element.IsMouseCaptured)
        {
            element.ReleaseMouseCapture();
        }

        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        e.Handled = true;
    }

    private void ShortcutItem_OnDragOver(object sender, DragEventArgs e)
    {
        if (!_viewModel.IsEditingShortcuts)
        {
            return;
        }

        if (e.Data.GetDataPresent(typeof(ShortcutItem)))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void ShortcutModule_OnPreviewDragEnter(object sender, DragEventArgs e) =>
        UpdateShortcutFileDropFeedback(e);

    private void ShortcutModule_OnPreviewDragOver(object sender, DragEventArgs e) =>
        UpdateShortcutFileDropFeedback(e);

    private void ShortcutModule_OnPreviewDragLeave(object sender, DragEventArgs e)
    {
        if (!IsShortcutFileDrop(e))
        {
            return;
        }

        _viewModel.IsShortcutDropTargetActive = false;
        e.Handled = true;
    }

    private async void ShortcutModule_OnPreviewDrop(object sender, DragEventArgs e)
    {
        if (!IsShortcutFileDrop(e))
        {
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
        _viewModel.IsShortcutDropTargetActive = false;

        var paths = GetFileDropPaths(e);
        await _viewModel.AddShortcutsFromPathsAsync(paths);
    }

    private void UpdateShortcutFileDropFeedback(DragEventArgs e)
    {
        if (!IsShortcutFileDrop(e))
        {
            return;
        }

        e.Effects = DragDropEffects.Copy;
        _viewModel.IsShortcutDropTargetActive = true;
        e.Handled = true;
    }

    private static bool IsShortcutFileDrop(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop) && GetFileDropPaths(e).Count > 0;

    private static IReadOnlyList<string> GetFileDropPaths(DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return [];
        }

        return paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();
    }

    private void ShortcutAddPopup_OnClosed(object? sender, EventArgs e)
    {
        _viewModel.CloseShortcutAddMenusCommand.Execute(null);
    }

    private async void ShortcutItem_OnDrop(object sender, DragEventArgs e)
    {
        if (!_viewModel.IsEditingShortcuts)
        {
            return;
        }

        var source = e.Data.GetData(typeof(ShortcutItem)) as ShortcutItem;
        var target = (sender as FrameworkElement)?.DataContext as ShortcutItem;
        await _viewModel.MoveShortcutAsync(source, target);
        e.Handled = true;
    }

    private static bool IsInside<T>(DependencyObject? source) where T : DependencyObject
    {
        var current = source;
        while (current != null)
        {
            if (current is T)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowPosition();
        _ = _settingsService.FlushPendingSavesAsync();

        if (AllowShutdown)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void SaveWindowPosition() => _viewModel.SaveWindowPosition(Left, Top);

    private void ClampToVisibleWorkArea()
    {
        var workLeft = SystemParameters.VirtualScreenLeft;
        var workTop = SystemParameters.VirtualScreenTop;
        var workRight = workLeft + SystemParameters.VirtualScreenWidth;
        var workBottom = workTop + SystemParameters.VirtualScreenHeight;

        var width = double.IsNaN(Width) || Width <= 0 ? ActualWidth : Width;
        var height = double.IsNaN(Height) || Height <= 0 ? ActualHeight : Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        Left = Math.Clamp(Left, workLeft, Math.Max(workLeft, workRight - width));
        Top = Math.Clamp(Top, workTop, Math.Max(workTop, workBottom - height));
    }
}
