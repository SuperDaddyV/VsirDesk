using CommunityToolkit.Mvvm.ComponentModel;
using UniDesk.Models;

namespace UniDesk.ViewModels;

public partial class ModuleSettingOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _moduleId = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private int _sortOrder;

    [ObservableProperty]
    private bool _canMoveUp;

    [ObservableProperty]
    private bool _canMoveDown;

    public static ModuleSettingOptionViewModel FromModel(ModuleSetting module) => new()
    {
        ModuleId = module.ModuleId,
        DisplayName = module.DisplayName,
        IsEnabled = module.IsEnabled,
        SortOrder = module.SortOrder
    };

    public ModuleSetting ToModel() => new()
    {
        ModuleId = ModuleId,
        DisplayName = DisplayName,
        IsEnabled = IsEnabled,
        SortOrder = SortOrder
    };
}
