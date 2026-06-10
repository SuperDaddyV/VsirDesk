using UniDesk.Models;

namespace UniDesk.Services;

public interface ISystemMetricsService
{
    SystemMetricsSnapshot Read();
}
