using System.Windows;

namespace UniDesk.Helpers;

public static class ToastPlacementHelper
{
    private const double ToastGap = 10;
    private const double DefaultTopOffset = 52;

    public static Window? GetAnchorWindow()
    {
        var main = Application.Current?.MainWindow;
        if (main is { IsLoaded: true })
        {
            return main;
        }

        return null;
    }

    public static void PositionNearAnchor(Window window, double topOffset = DefaultTopOffset, double stackIndex = 0)
    {
        window.UpdateLayout();

        var width = double.IsNaN(window.ActualWidth) || window.ActualWidth <= 0
            ? window.Width
            : window.ActualWidth;
        var height = double.IsNaN(window.ActualHeight) || window.ActualHeight <= 0
            ? window.Height
            : window.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            window.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = window.DesiredSize.Width;
            height = window.DesiredSize.Height;
        }

        var work = SystemParameters.WorkArea;
        var anchor = GetAnchorWindow();

        double left;
        double top;

        if (anchor != null)
        {
            left = anchor.Left - width - ToastGap;
            top = anchor.Top + topOffset + stackIndex * (height + 8);

            if (left < work.Left + 8)
            {
                left = anchor.Left + 12;
            }
        }
        else
        {
            left = work.Right - width - 24;
            top = work.Top + topOffset + stackIndex * (height + 8);
        }

        left = Math.Clamp(left, work.Left + 8, work.Right - width - 8);
        top = Math.Clamp(top, work.Top + 8, work.Bottom - height - 8);

        window.Left = left;
        window.Top = top;
    }

    public static void PositionConfirmNearAnchor(Window window, double topOffset = 72)
    {
        window.UpdateLayout();

        var width = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        var height = window.ActualHeight > 0 ? window.ActualHeight : window.Height;

        if (width <= 0 || height <= 0)
        {
            window.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = window.DesiredSize.Width;
            height = window.DesiredSize.Height;
        }

        var work = SystemParameters.WorkArea;
        var anchor = GetAnchorWindow();

        double left;
        double top;

        if (anchor != null)
        {
            left = anchor.Left + (anchor.ActualWidth - width) / 2;
            top = anchor.Top + topOffset;
        }
        else
        {
            left = work.Right - width - 80;
            top = work.Top + topOffset;
        }

        left = Math.Clamp(left, work.Left + 8, work.Right - width - 8);
        top = Math.Clamp(top, work.Top + 8, work.Bottom - height - 8);

        window.Left = left;
        window.Top = top;
    }
}
