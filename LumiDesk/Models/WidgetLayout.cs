namespace LumiDesk.Models;

public class WidgetLayout
{
    public string WidgetKey { get; set; } = string.Empty;   // ClockWeather/Shortcuts/Todos/Notes
    public int Order { get; set; }
    public double Height { get; set; }
    public bool IsLocked { get; set; } = true;
}
