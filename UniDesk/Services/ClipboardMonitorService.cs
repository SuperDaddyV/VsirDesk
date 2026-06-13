using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using UniDesk.Helpers;

namespace UniDesk.Services;

public sealed class ClipboardMonitorService : IClipboardMonitorService
{
    private const int WmClipboardUpdate = 0x031D;

    private readonly IQuickTextService _quickTextService;
    private HwndSource? _source;
    private nint _handle;
    private bool _isStarted;
    private bool _ignoreNextChange;

    public event Action? ClipboardHistoryChanged;

    public ClipboardMonitorService(IQuickTextService quickTextService)
    {
        _quickTextService = quickTextService;
    }

    public void Start(Window window)
    {
        if (_isStarted)
        {
            return;
        }

        try
        {
            _handle = new WindowInteropHelper(window).Handle;
            if (_handle == 0)
            {
                return;
            }

            _source = HwndSource.FromHwnd(_handle);
            _source?.AddHook(WndProc);
            if (AddClipboardFormatListener(_handle))
            {
                _isStarted = true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ClipboardMonitorService.Start");
        }
    }

    public void Stop()
    {
        try
        {
            if (_isStarted && _handle != 0)
            {
                RemoveClipboardFormatListener(_handle);
            }

            _source?.RemoveHook(WndProc);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ClipboardMonitorService.Stop");
        }
        finally
        {
            _isStarted = false;
            _source = null;
            _handle = 0;
        }
    }

    public bool TrySetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            _ignoreNextChange = true;
            Clipboard.SetText(text);
            return true;
        }
        catch (Exception ex)
        {
            _ignoreNextChange = false;
            Logger.LogError(ex, "ClipboardMonitorService.TrySetText");
            return false;
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmClipboardUpdate)
        {
            handled = false;
            _ = HandleClipboardUpdateAsync();
        }

        return 0;
    }

    private async Task HandleClipboardUpdateAsync()
    {
        if (_ignoreNextChange)
        {
            _ignoreNextChange = false;
            return;
        }

        string? text;
        try
        {
            if (!Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                return;
            }

            text = Clipboard.GetText(TextDataFormat.UnicodeText);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"剪贴板读取失败：{ex.Message}", "ClipboardMonitorService.HandleClipboardUpdate");
            return;
        }

        var recorded = await _quickTextService.RecordClipboardTextAsync(text);
        if (recorded)
        {
            ClipboardHistoryChanged?.Invoke();
        }
    }

    public void Dispose() => Stop();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(nint hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(nint hwnd);
}
