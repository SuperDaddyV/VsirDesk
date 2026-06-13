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
    private readonly IQuickNoteService _quickNoteService;
    private readonly IQuickTextService _quickTextService;
    private readonly IDatabaseService _databaseService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public TodoBackupService(
        ITodoService todoService,
        IQuickNoteService quickNoteService,
        IQuickTextService quickTextService,
        IDatabaseService databaseService)
    {
        _todoService = todoService;
        _quickNoteService = quickNoteService;
        _quickTextService = quickTextService;
        _databaseService = databaseService;
    }

    public async Task ExportToFileAsync(string filePath)
    {
        var todos = await _todoService.GetAllTodosAsync();
        var quickNotes = await _quickNoteService.GetAllQuickNotesAsync();
        var clipboardHistory = await _quickTextService.GetClipboardHistoryAsync(10_000);
        var textSnippets = await _quickTextService.GetTextSnippetsAsync();
        var payload = new TodoBackupFile
        {
            Version = 3,
            ExportedAt = DateTime.UtcNow,
            Todos = todos.Select(TodoBackupEntry.FromTodo).ToList(),
            QuickNotes = quickNotes.Select(QuickNoteBackupEntry.FromQuickNote).ToList(),
            ClipboardHistory = clipboardHistory.Select(ClipboardHistoryBackupEntry.FromHistory).ToList(),
            TextSnippets = textSnippets.Select(TextSnippetBackupEntry.FromSnippet).ToList()
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, Utf8NoBom);
    }

    public async Task<TodoBackupImportResult> ImportFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath, Utf8NoBom);
        var payload = JsonSerializer.Deserialize<TodoBackupFile>(json, JsonOptions)
                      ?? throw new InvalidDataException("备份文件格式无效。");

        if ((payload.Todos == null || payload.Todos.Count == 0) &&
            (payload.QuickNotes == null || payload.QuickNotes.Count == 0) &&
            (payload.ClipboardHistory == null || payload.ClipboardHistory.Count == 0) &&
            (payload.TextSnippets == null || payload.TextSnippets.Count == 0))
        {
            throw new InvalidDataException("备份文件中没有可还原的数据。");
        }

        var result = new TodoBackupImportResult();

        if (payload.Todos is { Count: > 0 })
        {
            await _databaseService.ExecuteNonQueryAsync("DELETE FROM Todos");

            foreach (var entry in payload.Todos)
            {
                if (string.IsNullOrWhiteSpace(entry.Title))
                {
                    continue;
                }

                await _todoService.CreateTodoAsync(entry.ToTodo());
                result.TodoCount++;
            }
        }

        if (payload.QuickNotes is { Count: > 0 })
        {
            await _databaseService.ExecuteNonQueryAsync("DELETE FROM QuickNotes");

            foreach (var entry in payload.QuickNotes)
            {
                if (string.IsNullOrWhiteSpace(entry.Title) && string.IsNullOrWhiteSpace(entry.Content))
                {
                    continue;
                }

                await _quickNoteService.CreateQuickNoteAsync(entry.ToQuickNote());
                result.QuickNoteCount++;
            }
        }

        if (payload.ClipboardHistory is { Count: > 0 })
        {
            await _databaseService.ExecuteNonQueryAsync("DELETE FROM ClipboardHistory");

            foreach (var entry in payload.ClipboardHistory)
            {
                var content = QuickTextService.NormalizeClipboardText(entry.Content);
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var hash = string.IsNullOrWhiteSpace(entry.ContentHash)
                    ? QuickTextService.ComputeHash(content)
                    : entry.ContentHash;
                var now = DateTime.UtcNow;
                var createdAt = entry.CreatedAt == default ? now : entry.CreatedAt;
                var lastUsedAt = entry.LastUsedAt == default ? createdAt : entry.LastUsedAt;

                await _databaseService.ExecuteNonQueryAsync(
                    "INSERT OR IGNORE INTO ClipboardHistory (Content, ContentHash, CreatedAt, LastUsedAt, UseCount) VALUES (@p0, @p1, @p2, @p3, @p4)",
                    content,
                    hash,
                    createdAt.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                    lastUsedAt.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                    Math.Max(1, entry.UseCount));
                result.ClipboardHistoryCount++;
            }
        }

        if (payload.TextSnippets is { Count: > 0 })
        {
            await _databaseService.ExecuteNonQueryAsync("DELETE FROM TextSnippets");

            foreach (var entry in payload.TextSnippets)
            {
                if (string.IsNullOrWhiteSpace(entry.Content))
                {
                    continue;
                }

                await _quickTextService.CreateTextSnippetAsync(entry.ToSnippet());
                result.TextSnippetCount++;
            }
        }

        return result;
    }

    private sealed class TodoBackupFile
    {
        public int Version { get; set; }
        public DateTime ExportedAt { get; set; }
        public List<TodoBackupEntry> Todos { get; set; } = [];
        public List<QuickNoteBackupEntry> QuickNotes { get; set; } = [];
        public List<ClipboardHistoryBackupEntry> ClipboardHistory { get; set; } = [];
        public List<TextSnippetBackupEntry> TextSnippets { get; set; } = [];
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

    private sealed class QuickNoteBackupEntry
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static QuickNoteBackupEntry FromQuickNote(QuickNote note) => new()
        {
            Title = note.Title,
            Content = note.Content,
            IsPinned = note.IsPinned,
            SortOrder = note.SortOrder,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };

        public QuickNote ToQuickNote()
        {
            var now = DateTime.UtcNow;
            return new QuickNote
            {
                Title = Title ?? string.Empty,
                Content = Content ?? string.Empty,
                IsPinned = IsPinned,
                SortOrder = SortOrder,
                CreatedAt = CreatedAt == default ? now : CreatedAt,
                UpdatedAt = UpdatedAt == default ? now : UpdatedAt
            };
        }
    }

    private sealed class ClipboardHistoryBackupEntry
    {
        public string Content { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsedAt { get; set; }
        public int UseCount { get; set; }

        public static ClipboardHistoryBackupEntry FromHistory(ClipboardHistoryItem item) => new()
        {
            Content = item.Content,
            ContentHash = item.ContentHash,
            CreatedAt = item.CreatedAt,
            LastUsedAt = item.LastUsedAt,
            UseCount = item.UseCount
        };
    }

    private sealed class TextSnippetBackupEntry
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = "默认";
        public bool IsPinned { get; set; }
        public int SortOrder { get; set; }
        public int UseCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }

        public static TextSnippetBackupEntry FromSnippet(TextSnippet snippet) => new()
        {
            Title = snippet.Title,
            Content = snippet.Content,
            Category = snippet.Category,
            IsPinned = snippet.IsPinned,
            SortOrder = snippet.SortOrder,
            UseCount = snippet.UseCount,
            CreatedAt = snippet.CreatedAt,
            UpdatedAt = snippet.UpdatedAt,
            LastUsedAt = snippet.LastUsedAt
        };

        public TextSnippet ToSnippet()
        {
            var now = DateTime.UtcNow;
            return new TextSnippet
            {
                Title = Title ?? string.Empty,
                Content = Content ?? string.Empty,
                Category = string.IsNullOrWhiteSpace(Category) ? "默认" : Category,
                IsPinned = IsPinned,
                SortOrder = SortOrder,
                UseCount = Math.Max(0, UseCount),
                CreatedAt = CreatedAt == default ? now : CreatedAt,
                UpdatedAt = UpdatedAt == default ? now : UpdatedAt,
                LastUsedAt = LastUsedAt
            };
        }
    }
}
