using Microsoft.Data.Sqlite;
using UniDesk.Helpers;

namespace UniDesk.Services;

public class DatabaseService : IDatabaseService
{
    private const string DatabaseVersion = "1.3";
    private readonly string _connectionString;

    public DatabaseService(string? connectionString = null)
    {
        DirectoryHelper.EnsureDirectoriesExist();
        _connectionString = connectionString ?? $"Data Source={DirectoryHelper.DatabaseFile}";
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var version = await GetDatabaseVersionAsync(connection);
            if (version == null)
            {
                await CreateTablesAsync(connection);
                await SetDatabaseVersionAsync(connection, DatabaseVersion);
            }
            else if (version != DatabaseVersion)
            {
                await MigrateDatabaseAsync(connection, version, DatabaseVersion);
                await SetDatabaseVersionAsync(connection, DatabaseVersion);
            }

            await EnsureSchemaUpdatesAsync(connection);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DatabaseService.Initialize");
        }
    }

    private async Task<string?> GetDatabaseVersionAsync(SqliteConnection connection)
    {
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = 'DatabaseVersion'";
            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task SetDatabaseVersionAsync(SqliteConnection connection, string version)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Settings (Key, Value) 
            VALUES ('DatabaseVersion', @version)";
        command.Parameters.AddWithValue("@version", version);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateTablesAsync(SqliteConnection connection)
    {
        var commands = new[]
        {
            @"
            CREATE TABLE IF NOT EXISTS Notes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Content TEXT,
                Color TEXT NOT NULL DEFAULT '#FFFFFF',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )",
            "CREATE INDEX IF NOT EXISTS idx_notes_updated_at ON Notes(UpdatedAt DESC)",
            @"
            CREATE TABLE IF NOT EXISTS Todos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                DueDate TEXT,
                CreatedAt TEXT NOT NULL,
                CompletedAt TEXT,
                Priority INTEGER NOT NULL DEFAULT 1
            )",
            "CREATE INDEX IF NOT EXISTS idx_todos_due_date ON Todos(DueDate)",
            "CREATE INDEX IF NOT EXISTS idx_todos_created_at ON Todos(CreatedAt)",
            @"
            CREATE TABLE IF NOT EXISTS Shortcuts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL,
                Type TEXT NOT NULL DEFAULT 'Application',
                IconPath TEXT,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                LaunchArguments TEXT
            )",
            "CREATE INDEX IF NOT EXISTS idx_shortcuts_sort_order ON Shortcuts(SortOrder)",
            @"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            )"
        };

        foreach (var sql in commands)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        await InitializeDefaultSettingsAsync(connection);
    }

    private async Task InitializeDefaultSettingsAsync(SqliteConnection connection)
    {
        var defaultSettings = new Dictionary<string, string>
        {
            { "Theme", "System" },
            { "ColorScheme", "Taro" },
            { "WindowOpacity", "0.70" },
            { "TopMost", "true" },
            { "Startup", "false" },
            { "AutoLocation", "true" },
            { "City", "" },
            { "PanelWidth", "320" },
            { "WindowLocked", "false" },
            { "PanelCollapsed", "false" },
            { "WindowLeft", "" },
            { "WindowTop", "" },
            { "WidgetLayout", "" },
            { "Hotkey", "Ctrl+Alt+Space" },
            { "WeatherApiKey", "" },
            { "WeatherApiHost", "" },
            { WeatherApiDefaults.DefaultApiKeySettingKey, WeatherApiDefaults.BuiltInApiKeyEncrypted },
            { WeatherApiDefaults.DefaultApiHostSettingKey, WeatherApiDefaults.BuiltInApiHostEncrypted },
            { "ShortcutMaxCount", "9" }
        };

        foreach (var setting in defaultSettings)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO Settings (Key, Value) 
                VALUES (@key, @value)";
            command.Parameters.AddWithValue("@key", setting.Key);
            command.Parameters.AddWithValue("@value", setting.Value);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task MigrateDatabaseAsync(SqliteConnection connection, string fromVersion, string toVersion)
    {
        if (fromVersion == "1.0" && toVersion == "1.1")
        {
            await TryAddColumnAsync(connection, "Todos", "Priority", "INTEGER NOT NULL DEFAULT 1");
        }

        if (string.Compare(fromVersion, "1.2", StringComparison.Ordinal) < 0 &&
            string.Compare(toVersion, "1.2", StringComparison.Ordinal) >= 0)
        {
            await TryAddColumnAsync(connection, "Shortcuts", "LaunchArguments", "TEXT");
        }

        if (string.Compare(fromVersion, "1.3", StringComparison.Ordinal) < 0 &&
            string.Compare(toVersion, "1.3", StringComparison.Ordinal) >= 0)
        {
            await EnsureEncryptedWeatherDefaultsAsync(connection);
        }
    }

    private async Task EnsureSchemaUpdatesAsync(SqliteConnection connection)
    {
        await TryAddColumnAsync(connection, "Todos", "Priority", "INTEGER NOT NULL DEFAULT 1");
        await TryAddColumnAsync(connection, "Shortcuts", "LaunchArguments", "TEXT");
        await EnsureEncryptedWeatherDefaultsAsync(connection);
    }

    private static async Task EnsureEncryptedWeatherDefaultsAsync(SqliteConnection connection)
    {
        var defaults = new Dictionary<string, string>
        {
            { WeatherApiDefaults.DefaultApiKeySettingKey, WeatherApiDefaults.BuiltInApiKeyEncrypted },
            { WeatherApiDefaults.DefaultApiHostSettingKey, WeatherApiDefaults.BuiltInApiHostEncrypted }
        };

        foreach (var setting in defaults)
        {
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT OR IGNORE INTO Settings (Key, Value)
                VALUES (@key, @value)";
            insertCommand.Parameters.AddWithValue("@key", setting.Key);
            insertCommand.Parameters.AddWithValue("@value", setting.Value);
            await insertCommand.ExecuteNonQueryAsync();

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE Settings
                SET Value = @value
                WHERE Key = @key AND (Value IS NULL OR trim(Value) = '')";
            updateCommand.Parameters.AddWithValue("@key", setting.Key);
            updateCommand.Parameters.AddWithValue("@value", setting.Value);
            await updateCommand.ExecuteNonQueryAsync();
        }
    }

    private static async Task TryAddColumnAsync(SqliteConnection connection, string table, string column, string definition)
    {
        var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table})";
        var hasColumn = false;
        await using (var reader = await check.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (hasColumn) return;

        var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        await alter.ExecuteNonQueryAsync();
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, params object?[] parameters)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = sql;

        for (int i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
        }

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> map, params object?[] parameters)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = sql;

        for (int i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
        }

        var results = new List<T>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }

    public async Task<T?> QuerySingleAsync<T>(string sql, Func<SqliteDataReader, T> map, params object?[] parameters)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = sql;

        for (int i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
        }

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return map(reader);
        }

        return default;
    }
}
