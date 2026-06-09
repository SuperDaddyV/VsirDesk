using Xunit;
using LumiDesk.Services;
using LumiDesk.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using LumiDesk.Helpers;

namespace LumiDesk.Tests;

[Collection("Database Tests")]
public class TodoServiceTests
{
    private string _testDbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_todo.db");

    private async Task<(DatabaseService db, TodoService svc)> InitAsync()
    {
        var connectionString = $"Data Source={_testDbFile}";
        var db = new DatabaseService(connectionString);
        await db.InitializeAsync();
        var svc = new TodoService(db);
        return (db, svc);
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
        }
        catch
        {
        }
    }

    [Fact]
    public async Task CreateTodoAsync_ShouldInsertAndReturnId()
    {
        var (db, svc) = await InitAsync();

        var todo = new TodoItem
        {
            Title = "Test Todo",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var id = await svc.CreateTodoAsync(todo);
        Assert.True(id > 0);

        var fetched = await svc.GetTodoAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal("Test Todo", fetched!.Title);
        Assert.False(fetched.IsCompleted);

        Cleanup();
    }

    [Fact]
    public async Task ToggleCompleteAsync_ShouldChangeStatus()
    {
        var (db, svc) = await InitAsync();

        var todo = new TodoItem
        {
            Title = "Toggle Test",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var id = await svc.CreateTodoAsync(todo);
        Assert.True(id > 0);
        
        await svc.ToggleCompleteAsync(id);
        var fetched = await svc.GetTodoAsync(id);
        Assert.NotNull(fetched);
        Assert.True(fetched!.IsCompleted);
        Assert.NotNull(fetched.CompletedAt);

        await svc.ToggleCompleteAsync(id);
        fetched = await svc.GetTodoAsync(id);
        Assert.NotNull(fetched);
        Assert.False(fetched!.IsCompleted);
        Assert.Null(fetched.CompletedAt);

        Cleanup();
    }

    [Fact]
    public async Task GetTodayTodosAsync_ShouldReturnRelevantTodos()
    {
        var (db, svc) = await InitAsync();

        await svc.CreateTodoAsync(new TodoItem { Title = "Today", DueDate = DateTime.UtcNow });
        await svc.CreateTodoAsync(new TodoItem { Title = "No Due Date", IsCompleted = false });
        await svc.CreateTodoAsync(new TodoItem { Title = "Tomorrow", DueDate = DateTime.UtcNow.AddDays(1) });

        var todayTodos = await svc.GetTodayTodosAsync();
        
        Assert.Contains(todayTodos, t => t.Title == "Today");
        Assert.Contains(todayTodos, t => t.Title == "No Due Date");
        Assert.DoesNotContain(todayTodos, t => t.Title == "Tomorrow");

        Cleanup();
    }

    [Fact]
    public async Task DeleteTodoAsync_ShouldRemoveTodo()
    {
        var (db, svc) = await InitAsync();

        var id = await svc.CreateTodoAsync(new TodoItem { Title = "Delete Me" });
        Assert.True(id > 0);
        Assert.NotNull(await svc.GetTodoAsync(id));

        await svc.DeleteTodoAsync(id);
        Assert.Null(await svc.GetTodoAsync(id));

        Cleanup();
    }
}
