using System.IO;

namespace LumiDesk.Helpers;

public static class Logger
{
    private static readonly object Sync = new();

    public static void LogError(Exception ex, string source = "Error")
    {
        Write("ERROR", source, $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }

    public static void LogWarning(string message, string source = "Warning")
    {
        Write("WARN", source, message);
    }

    public static void LogInfo(string message, string source = "Info")
    {
        Write("INFO", source, message);
    }

    private static void Write(string level, string source, string message)
    {
        try
        {
            DirectoryHelper.EnsureDirectoriesExist();
            var logFile = Path.Combine(DirectoryHelper.LogsDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}{Environment.NewLine}";

            lock (Sync)
            {
                File.AppendAllText(logFile, line);
            }
        }
        catch
        {
        }
    }
}
