using UniDesk.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace UniDesk;

public partial class NoteEditWindow : Window
{
    private readonly NoteEditViewModel _viewModel;

    public NoteEditWindow(NoteEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var ok = await _viewModel.SaveAsync();
        if (!ok) return;

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void NoteEditWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;

        DialogResult = false;
        Close();
    }
}
