namespace UniDesk.Models;

public sealed class SystemMetricsSnapshot
{
    public double? CpuUsage { get; init; }
    public double? CpuTemperature { get; init; }
    public double? MemoryUsage { get; init; }
    public double? GpuUsage { get; init; }
    public double? GpuTemperature { get; init; }
    public double? NetworkReceivedBytesPerSecond { get; init; }
    public double? NetworkSentBytesPerSecond { get; init; }
}
