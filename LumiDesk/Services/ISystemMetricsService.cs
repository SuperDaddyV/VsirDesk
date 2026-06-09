using LumiDesk.Models;

namespace LumiDesk.Services;

public interface ISystemMetricsService
{
    SystemMetricsSnapshot Read();
}
