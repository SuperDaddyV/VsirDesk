using System.Windows.Threading;

namespace UniDesk.Helpers;

/// <summary>
/// Coalesces rapid repeated calls on the UI thread (default 75ms).
/// </summary>
public sealed class Debouncer : IDisposable
{
    private readonly DispatcherTimer _timer;
    private Action? _pendingAction;

    public Debouncer(TimeSpan? interval = null)
    {
        _timer = new DispatcherTimer
        {
            Interval = interval ?? TimeSpan.FromMilliseconds(75)
        };
        _timer.Tick += OnTick;
    }

    public void Schedule(Action action)
    {
        _pendingAction = action;
        _timer.Stop();
        _timer.Start();
    }

    public void Flush()
    {
        _timer.Stop();
        RunPending();
    }

    public void Cancel()
    {
        _timer.Stop();
        _pendingAction = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        RunPending();
    }

    private void RunPending()
    {
        var action = _pendingAction;
        _pendingAction = null;
        action?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
        _pendingAction = null;
    }
}
