namespace LumiDesk.Models;

public class CalendarDayItem
{
    public int Day { get; init; }
    public string LunarText { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public bool IsCurrentMonth { get; init; }
    public bool IsSelected { get; init; }
    public bool IsToday { get; init; }
}
