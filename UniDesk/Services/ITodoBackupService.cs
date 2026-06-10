namespace UniDesk.Services;

public interface ITodoBackupService
{
    Task ExportToFileAsync(string filePath);
    Task<int> ImportFromFileAsync(string filePath);
}
