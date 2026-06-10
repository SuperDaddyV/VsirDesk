namespace UniDesk.Models;

public class ClockInfo
{
    public string Time { get; set; } = string.Empty;        // HH:mm:ss
    public string Date { get; set; } = string.Empty;        // yyyy年MM月dd日
    public string DayOfWeek { get; set; } = string.Empty;   // 星期X
    public bool IsError { get; set; }
}
