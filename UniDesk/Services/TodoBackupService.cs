using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniDesk.Models;

namespace UniDesk.Services;

public class TodoBackupService : ITodoBackupService
{
    private readonly ITodoService _todoService;
    private readonly IDatabaseService _databaseService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public TodoBackupService(ITodoService todoService, IDatabaseService databaseService)
    {
        _todoService = todoService;
        _databaseService = databaseService;
    }

    public async Task ExportToFileAsync(string filePath)
    {
        var todos = await _todoService.GetAllTodosAsync();
        var payload = new TodoBackupFile
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Todos = todos.Select(TodoBackupEntry.FromTodo).ToList()
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, Utf8NoBom);
    }

    public async Task<int> ImportFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath, Utf8NoBom);
        var payload = JsonSerializer.Deserialize<TodoBackupFile>(json, JsonOptions)
                      ?? throw new InvalidDataException("备份文件格式无效。");

        if (payload.Todos == null || payload.Todos.Count == 0)
        {
            throw new InvalidDataException("备份文件中没有待办事项。");
        }

        await _databaseService.ExecuteNonQueryAsync("DELETE FROM Todos");

        var imported = 0;
        foreach (var entry in payload.Todos)
        {
            if (string.IsNullOrWhiteSpace(entry.Title))
            {
                continue;
            }

            await _todoService.CreateTodoAsync(entry.ToTodo());
            imported++;
        }

        return imported;
    }

    private sealed class TodoBackupFile
    {
        public int Version { get; set; }
        public DateTime ExportedAt { get; set; }
        public List<TodoBackupEntry> Todos { get; set; } = [];
    }

    private sealed class TodoBackupEntry
    {
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public TodoPriority Priority { get; set; } = TodoPriority.Medium;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public static TodoBackupEntry FromTodo(TodoItem todo) => new()
        {
            Title = todo.Title,
            IsCompleted = todo.IsCompleted,
            DueDate = todo.DueDate,
            Priority = todo.Priority,
            CreatedAt = todo.CreatedAt,
            CompletedAt = todo.CompletedAt
        };

        public TodoItem ToTodo() => new()
        {
            Title = Title,
            IsCompleted = IsCompleted,
            DueDate = DueDate,
            Priority = Priority,
            CreatedAt = CreatedAt == default ? DateTime.UtcNow : CreatedAt,
            CompletedAt = CompletedAt
        };
    }
}
