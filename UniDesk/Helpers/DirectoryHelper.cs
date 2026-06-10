namespace UniDesk.Helpers;

public static class DirectoryHelper
{
    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string AppDataPath = System.IO.Path.Combine(LocalAppData, "UniDesk");
    private static readonly string LegacyAppDataPath = System.IO.Path.Combine(LocalAppData, "LumiDesk");

    public static string AppData => AppDataPath;
    public static string DataDirectory => AppDataPath;
    public static string DatabaseFile => System.IO.Path.Combine(AppDataPath, "UniDesk.db");
    public static string IconsDirectory => System.IO.Path.Combine(AppDataPath, "icons");
    public static string LogsDirectory => System.IO.Path.Combine(AppDataPath, "logs");
    public static string CacheDirectory => System.IO.Path.Combine(AppDataPath, "cache");

    public static void EnsureDirectoriesExist()
    {
        MigrateLegacyDataIfNeeded();

        if (!System.IO.Directory.Exists(AppDataPath))
            System.IO.Directory.CreateDirectory(AppDataPath);

        if (!System.IO.Directory.Exists(IconsDirectory))
            System.IO.Directory.CreateDirectory(IconsDirectory);

        if (!System.IO.Directory.Exists(LogsDirectory))
            System.IO.Directory.CreateDirectory(LogsDirectory);

        if (!System.IO.Directory.Exists(CacheDirectory))
            System.IO.Directory.CreateDirectory(CacheDirectory);
    }

    private static void MigrateLegacyDataIfNeeded()
    {
        if (!System.IO.Directory.Exists(LegacyAppDataPath))
        {
            return;
        }

        try
        {
            if (!System.IO.Directory.Exists(AppDataPath))
            {
                System.IO.Directory.CreateDirectory(AppDataPath);
            }

            CopyDirectoryWithoutOverwrite(LegacyAppDataPath, AppDataPath);

            var legacyDatabase = System.IO.Path.Combine(LegacyAppDataPath, "LumiDesk.db");
            if (System.IO.File.Exists(legacyDatabase) && !System.IO.File.Exists(DatabaseFile))
            {
                System.IO.File.Copy(legacyDatabase, DatabaseFile, overwrite: false);
            }
        }
        catch
        {
            // Startup must continue even if old user data cannot be migrated.
        }
    }

    private static void CopyDirectoryWithoutOverwrite(string sourceDirectory, string targetDirectory)
    {
        foreach (var directory in System.IO.Directory.EnumerateDirectories(sourceDirectory, "*", System.IO.SearchOption.AllDirectories))
        {
            var relativePath = System.IO.Path.GetRelativePath(sourceDirectory, directory);
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in System.IO.Directory.EnumerateFiles(sourceDirectory, "*", System.IO.SearchOption.AllDirectories))
        {
            var relativePath = System.IO.Path.GetRelativePath(sourceDirectory, file);
            var targetFile = System.IO.Path.Combine(targetDirectory, relativePath);
            var targetFileDirectory = System.IO.Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrWhiteSpace(targetFileDirectory))
            {
                System.IO.Directory.CreateDirectory(targetFileDirectory);
            }

            if (!System.IO.File.Exists(targetFile))
            {
                System.IO.File.Copy(file, targetFile, overwrite: false);
            }
        }
    }
}
