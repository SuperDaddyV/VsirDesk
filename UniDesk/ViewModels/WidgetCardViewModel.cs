using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniDesk.Models;
using UniDesk.Services;

namespace UniDesk.ViewModels;

public partial class WidgetCardViewModel : ObservableObject
{
    private readonly WidgetLayout _layout;
    private readonly ILayoutService _layoutService;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private bool _isDragging;

    public string WidgetKey => _layout.WidgetKey;
    public int Order => _layout.Order;

    public string Title => WidgetKey switch
    {
        "Clock" => "时钟",
        "Weather" => "天气",
        "Shortcuts" => "快捷启动",
        "Todos" => "待办",
        "Notes" => "便签",
        _ => WidgetKey
    };

    public WidgetCardViewModel(WidgetLayout layout, ILayoutService layoutService)
    {
        _layout = layout;
        _layoutService = layoutService;

        _isLocked = layout.IsLocked;
        _height = layout.Height;
    }

    partial void OnIsLockedChanged(bool value)
    {
        _layout.IsLocked = value;
    }

    partial void OnHeightChanged(double value)
    {
        if (value < 40)
        {
            Height = 40;
            return;
        }

        if (value > 600)
        {
            Height = 600;
            return;
        }

        _layout.Height = value;
    }

    [RelayCommand]
    private void ToggleLock()
    {
        IsLocked = !IsLocked;
    }

    public void UpdateLayout(WidgetLayout layout)
    {
        Height = layout.Height;
        IsLocked = layout.IsLocked;
    }
}