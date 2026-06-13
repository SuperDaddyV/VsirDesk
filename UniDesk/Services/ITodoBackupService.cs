namespace UniDesk.Services;

using UniDesk.Models;

public interface ITodoBackupService
{
    Task ExportToFileAsync(string filePath);
    Task<TodoBackupImportResult> ImportFromFileAsync(string filePath);
}
