using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UniDesk.Services;
using UniDesk.ViewModels;

namespace UniDesk;

public partial class TextSnippetEditWindow : Window
{
    private readonly TextSnippetEditViewModel _viewModel;

    public TextSnippetEditWindow(TextSnippetEditViewModel viewModel, double panelWidth)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        var width = panelWidth - 28;
        if (width < IWindowService.MinPanelWidth - 28)
        {
            width = IWindowService.MinPanelWidth - 28;
        }

        Width = width;
        MinWidth = width;
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!await _viewModel.SaveAsync())
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

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
            if (current is TextBoxBase or Button or ScrollBar or Thumb or CheckBox or ComboBox)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void TextSnippetEditWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        DialogResult = false;
        Close();
        e.Handled = true;
    }
}
