using UniDesk.Services;
using Xunit;

namespace UniDesk.Tests;

public class SystemMetricsServiceTests
{
    [Fact]
    public void Read_ShouldReturnSnapshotWithoutThrowing()
    {
        using var service = new SystemMetricsService();

        var snapshot = service.Read();

        Assert.NotNull(snapshot);
    }
}
