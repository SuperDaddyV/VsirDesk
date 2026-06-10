using System.Globalization;

namespace UniDesk.Helpers;

public static class TodoDisplayHelper
{
    private static readonly string[] WeekdayNames = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];

    public static string FormatDateWithWeekday(DateTime date)
        => $"{date:yyyy-MM-dd} ({WeekdayNames[(int)date.DayOfWeek]})";

    public static DateTime EndOfWeek(DateTime date)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
        return date.Date.AddDays(daysUntilSunday == 0 ? 7 : daysUntilSunday);
    }

    public static string FormatTime(DateTime dateTime)
        => dateTime.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static (DateTime Date, TimeSpan Time) SplitDateTime(DateTime dateTime)
        => (dateTime.Date, dateTime.TimeOfDay);
}
