using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UniDesk.Services;
using UniDesk.ViewModels;

namespace UniDesk;

public partial class QuickTextManagerWindow : Window
{
    public QuickTextManagerWindow(QuickTextManagerViewModel viewModel, double panelWidth)
    {
        InitializeComponent();
        DataContext = viewModel;

        var width = Math.Max(panelWidth - 20, IWindowService.MinPanelWidth - 20);
        Width = Math.Max(width, 420);
        MinWidth = Math.Min(Width, 420);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private void DialogRoot_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ShouldIgnoreDrag(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static bool ShouldIgnoreDrag(DependencyObject? source)
    {
        var current = source;
        while (current != null)
        {
            if (current is TextBoxBase or Button or ScrollBar or Thumb or ComboBox)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void QuickTextManagerWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Close();
        e.Handled = true;
    }
}
