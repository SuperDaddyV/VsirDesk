using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using LumiDesk.Services;

namespace LumiDesk.Helpers;

public class SingleInstanceHelper : IDisposable
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    private const string MutexName = "LumiDesk_SingleInstance_Mutex_6B9BD6F1-8E3A-4C5D-9F2B-1A7C8D3E5F9A";
    private const string MainWindowTitle = "LumiDesk";

    private Mutex? _mutex;
    private readonly INotificationService _notificationService;

    public bool IsFirstInstance { get; private set; }

    public SingleInstanceHelper(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            IsFirstInstance = false;
            TryActivateExistingInstance();
            return false;
        }

        IsFirstInstance = true;
        return true;
    }

    private void TryActivateExistingInstance()
    {
        var hWnd = FindWindow(null, MainWindowTitle);
        if (hWnd != IntPtr.Zero)
        {
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }
    }

    public void Release()
    {
        if (_mutex != null && IsFirstInstance)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            _mutex = null;
        }
    }

    public void Dispose()
    {
        Release();
    }
}
