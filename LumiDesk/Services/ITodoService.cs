using LumiDesk.Models;

namespace LumiDesk.Services;

public interface ITodoService
{
    Task<List<TodoItem>> GetAllTodosAsync();
    Task<TodoItem?> GetTodoAsync(int id);
    Task<int> CreateTodoAsync(TodoItem todo);
    Task UpdateTodoAsync(TodoItem todo);
    Task DeleteTodoAsync(int id);
    Task ToggleCompleteAsync(int id);
    Task MarkCompletedAsync(int id);
    Task MarkUncompletedAsync(int id);
    Task<List<TodoItem>> GetTodayTodosAsync();
}
