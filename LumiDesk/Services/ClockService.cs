using System.Windows.Threading;

namespace LumiDesk.Services;

public class ClockService : IClockService
{
    private readonly DispatcherTimer _timer = new();
    private DateTime _currentTime = DateTime.Now;

    public event Action? TimeChanged;

    public DateTime CurrentTime => _currentTime;

    public ClockService()
    {
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            _currentTime = DateTime.Now;
            TimeChanged?.Invoke();
        };
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }
}