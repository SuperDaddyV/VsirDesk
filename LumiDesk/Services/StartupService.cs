using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace LumiDesk.Services;

public class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "LumiDesk";
    private const string ScheduledTaskName = @"\LumiDesk";

    private readonly INotificationService _notificationService;

    public StartupService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public bool IsEnabled => IsRegisteredInTaskScheduler() || IsRegisteredInRunKey();

    public bool Enable()
    {
        try
        {
            var exePath = GetExecutablePath();
            if (string.IsNullOrEmpty(exePath))
            {
                _notificationService.ShowErrorMessage("无法获取程序路径，无法设置开机自启。");
                return false;
            }

            if (!CreateScheduledTask(exePath))
            {
                _notificationService.ShowErrorMessage("无法创建计划任务，开机自启设置失败。");
                return false;
            }

            DeleteRunKeyValue();
            return true;
        }
        catch (Exception ex)
        {
            _notificationService.ShowErrorMessage($"设置开机自启失败：{ex.Message}");
            return false;
        }
    }

    public bool Disable()
    {
        try
        {
            var removedTask = DeleteScheduledTask();
            var removedRunKey = DeleteRunKeyValue();
            return removedTask || removedRunKey || !IsEnabled;
        }
        catch (Exception ex)
        {
            _notificationService.ShowErrorMessage($"取消开机自启失败：{ex.Message}");
            return false;
        }
    }

    public void SyncWithSetting(bool shouldEnable)
    {
        if (shouldEnable)
        {
            if (!IsEnabled)
            {
                Enable();
            }
        }
        else if (IsEnabled)
        {
            Disable();
        }
    }

    private static bool IsRegisteredInRunKey()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(RegistryValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRegisteredInTaskScheduler()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var result = RunSchtasks($"/Query /TN \"{ScheduledTaskName}\"");
        return result.ExitCode == 0;
    }

    private static bool CreateScheduledTask(string exePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var taskRun = $"\\\"{exePath}\\\"";
        var args = $"/Create /TN \"{ScheduledTaskName}\" /SC ONLOGON /TR \"{taskRun}\" /RL LIMITED /F";
        return RunSchtasks(args).ExitCode == 0;
    }

    private static bool DeleteScheduledTask()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var result = RunSchtasks($"/Delete /TN \"{ScheduledTaskName}\" /F");
        return result.ExitCode == 0;
    }

    private static bool DeleteRunKeyValue()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key?.GetValue(RegistryValueName) == null)
            {
                return false;
            }

            key.DeleteValue(RegistryValueName, throwOnMissingValue: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (int ExitCode, string Output) RunSchtasks(string arguments)
    {
        try
        {
            var schtasksPath = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
            if (!File.Exists(schtasksPath))
            {
                schtasksPath = "schtasks.exe";
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = schtasksPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process == null)
            {
                return (-1, string.Empty);
            }

            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return (process.ExitCode, output);
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
    }

    private static string? GetExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return path;
        }

        path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            var exe = Path.ChangeExtension(path, ".exe");
            if (File.Exists(exe))
            {
                return exe;
            }
        }

        return File.Exists(path) ? path : null;
    }
}
