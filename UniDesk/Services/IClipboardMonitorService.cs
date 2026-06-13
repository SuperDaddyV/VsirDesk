using System.Windows;

namespace UniDesk.Services;

public interface IClipboardMonitorService : IDisposable
{
    event Action? ClipboardHistoryChanged;
    void Start(Window window);
    void Stop();
    bool TrySetText(string text);
}
