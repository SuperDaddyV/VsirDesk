namespace LumiDesk.Helpers;

public static class DirectoryHelper
{
    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string AppDataPath = System.IO.Path.Combine(LocalAppData, "LumiDesk");

    public static string AppData => AppDataPath;
    public static string DataDirectory => AppDataPath;
    public static string DatabaseFile => System.IO.Path.Combine(AppDataPath, "LumiDesk.db");
    public static string IconsDirectory => System.IO.Path.Combine(AppDataPath, "icons");
    public static string LogsDirectory => System.IO.Path.Combine(AppDataPath, "logs");
    public static string CacheDirectory => System.IO.Path.Combine(AppDataPath, "cache");

    public static void EnsureDirectoriesExist()
    {
        if (!System.IO.Directory.Exists(AppDataPath))
            System.IO.Directory.CreateDirectory(AppDataPath);

        if (!System.IO.Directory.Exists(IconsDirectory))
            System.IO.Directory.CreateDirectory(IconsDirectory);

        if (!System.IO.Directory.Exists(LogsDirectory))
            System.IO.Directory.CreateDirectory(LogsDirectory);

        if (!System.IO.Directory.Exists(CacheDirectory))
            System.IO.Directory.CreateDirectory(CacheDirectory);
    }
}