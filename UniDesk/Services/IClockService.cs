namespace UniDesk.Services;

public interface IClockService
{
    DateTime CurrentTime { get; }
    event Action? TimeChanged;
    void Start();
    void Stop();
}