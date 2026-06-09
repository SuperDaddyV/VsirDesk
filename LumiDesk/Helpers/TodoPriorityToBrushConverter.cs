using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LumiDesk.Models;

namespace LumiDesk.Helpers;

public class TodoPriorityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value is TodoPriority priority
            ? priority switch
            {
                TodoPriority.Low => "#94A3B8",
                TodoPriority.Medium => "#F59E0B",
                TodoPriority.High => "#EF4444",
                _ => "#94A3B8"
            }
            : "#94A3B8";

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
