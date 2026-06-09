using System.Globalization;
using System.Windows.Data;
using LumiDesk.Models;

namespace LumiDesk.Helpers;

public class TodoDueTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TodoItem todo || todo.DueDate == null)
        {
            return string.Empty;
        }

        var due = todo.DueDate.Value;
        var today = DateTime.Today;
        var hasTime = due.TimeOfDay.TotalSeconds > 0;

        if (due.Date == today)
        {
            return hasTime
                ? $"今天 {due:HH:mm}"
                : "今天";
        }

        if (due.Date == today.AddDays(1))
        {
            return hasTime
                ? $"明天 {due:HH:mm}"
                : "明天";
        }

        return hasTime
            ? due.ToString("M/d HH:mm", culture)
            : due.ToString("M/d", culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TodoDueIsTodayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TodoItem todo || todo.DueDate == null)
        {
            return false;
        }

        return todo.DueDate.Value.Date == DateTime.Today;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
