using System.Windows;
using System.Windows.Input;
using UniDesk.Helpers;

namespace UniDesk.Windows;

public partial class CompactConfirmWindow : Window
{
    public CompactConfirmWindow(string message, string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        DesktopWidgetWindowHelper.Configure(this);
    }

    public static bool Show(string message, string title)
    {
        var window = new CompactConfirmWindow(message, title);
        var anchor = ToastPlacementHelper.GetAnchorWindow();
        if (anchor != null)
        {
            window.Owner = anchor;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.ContentRendered += (_, _) => ToastPlacementHelper.PositionConfirmNearAnchor(window);
        return window.ShowDialog() == true;
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
