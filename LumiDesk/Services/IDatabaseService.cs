namespace LumiDesk.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<int> ExecuteNonQueryAsync(string sql, params object?[] parameters);
    Task<List<T>> QueryAsync<T>(string sql, Func<Microsoft.Data.Sqlite.SqliteDataReader, T> map, params object?[] parameters);
    Task<T?> QuerySingleAsync<T>(string sql, Func<Microsoft.Data.Sqlite.SqliteDataReader, T> map, params object?[] parameters);
}