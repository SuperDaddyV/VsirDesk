using CommunityToolkit.Mvvm.ComponentModel;
using LumiDesk.Helpers;
using System.Windows.Media;

namespace LumiDesk.ViewModels;

public partial class ColorSchemeOptionViewModel : ObservableObject
{
    public ColorSchemeOptionViewModel(AppColorScheme scheme)
    {
        Scheme = scheme;
        SwatchBrush = new SolidColorBrush(scheme.SwatchColor);
        SwatchBrush.Freeze();
    }

    public AppColorScheme Scheme { get; }

    public string Id => Scheme.Id;

    public string DisplayName => Scheme.DisplayName;

    public SolidColorBrush SwatchBrush { get; }

    [ObservableProperty]
    private bool _isSelected;
}
