using UniDesk.Models;
using UniDesk.Services;
using Xunit;

namespace UniDesk.Tests;

[Collection("Database Tests")]
public class TodoBackupServiceTests
{
    private readonly string _testDbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_backup.db");
    private readonly string _backupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_backup.json");

    [Fact]
    public async Task ExportAndImport_ShouldIncludeQuickNotes()
    {
        var (db, todoService, quickNoteService, quickTextService, backupService) = await InitAsync();

        await todoService.CreateTodoAsync(new TodoItem { Title = "待办" });
        await quickNoteService.CreateQuickNoteAsync(new QuickNote
        {
            Title = "便签",
            Content = "便签内容",
            IsPinned = true
        });
        await quickTextService.RecordClipboardTextAsync("剪贴板历史");
        await quickTextService.CreateTextSnippetAsync(new TextSnippet
        {
            Title = "短语",
            Content = "常用短语"
        });

        await backupService.ExportToFileAsync(_backupFile);
        await db.ExecuteNonQueryAsync("DELETE FROM Todos");
        await db.ExecuteNonQueryAsync("DELETE FROM QuickNotes");
        await db.ExecuteNonQueryAsync("DELETE FROM ClipboardHistory");
        await db.ExecuteNonQueryAsync("DELETE FROM TextSnippets");

        var result = await backupService.ImportFromFileAsync(_backupFile);

        Assert.Equal(1, result.TodoCount);
        Assert.Equal(1, result.QuickNoteCount);
        Assert.Equal(1, result.ClipboardHistoryCount);
        Assert.Equal(1, result.TextSnippetCount);
        Assert.Single(await todoService.GetAllTodosAsync());
        var notes = await quickNoteService.GetAllQuickNotesAsync();
        Assert.Single(notes);
        Assert.Equal("便签", notes[0].Title);
        Assert.Single(await quickTextService.GetClipboardHistoryAsync());
        Assert.Single(await quickTextService.GetTextSnippetsAsync());

        Cleanup();
    }

    [Fact]
    public async Task ImportFromFileAsync_ShouldAcceptOldTodoOnlyBackup()
    {
        var (_, todoService, quickNoteService, quickTextService, backupService) = await InitAsync();
        await File.WriteAllTextAsync(
            _backupFile,
            """
            {
              "version": 1,
              "exportedAt": "2026-06-13T00:00:00Z",
              "todos": [
                {
                  "title": "旧备份待办",
                  "isCompleted": false,
                  "priority": 1,
                  "createdAt": "2026-06-13T00:00:00Z"
                }
              ]
            }
            """);

        var result = await backupService.ImportFromFileAsync(_backupFile);

        Assert.Equal(1, result.TodoCount);
        Assert.Equal(0, result.QuickNoteCount);
        Assert.Equal(0, result.ClipboardHistoryCount);
        Assert.Equal(0, result.TextSnippetCount);
        Assert.Single(await todoService.GetAllTodosAsync());
        Assert.Empty(await quickNoteService.GetAllQuickNotesAsync());
        Assert.Empty(await quickTextService.GetClipboardHistoryAsync());
        Assert.Empty(await quickTextService.GetTextSnippetsAsync());

        Cleanup();
    }

    private async Task<(DatabaseService Db, TodoService TodoService, QuickNoteService QuickNoteService, QuickTextService QuickTextService, TodoBackupService BackupService)> InitAsync()
    {
        var db = new DatabaseService($"Data Source={_testDbFile}");
        await db.InitializeAsync();
        var todoService = new TodoService(db);
        var quickNoteService = new QuickNoteService(db);
        var settingsService = new SettingsService(db);
        var quickTextService = new QuickTextService(db, settingsService);
        var backupService = new TodoBackupService(todoService, quickNoteService, quickTextService, db);
        return (db, todoService, quickNoteService, quickTextService, backupService);
    }

    private void Cleanup()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_testDbFile))
            {
                File.Delete(_testDbFile);
            }

            if (File.Exists(_backupFile))
            {
                File.Delete(_backupFile);
            }
        }
        catch
        {
        }
    }
}
