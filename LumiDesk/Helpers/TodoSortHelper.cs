using LumiDesk.Models;

namespace LumiDesk.Helpers;

public static class TodoSortHelper
{
    public static IEnumerable<TodoItem> Sort(IEnumerable<TodoItem> todos)
    {
        return todos
            .OrderBy(t => t.IsCompleted)
            .ThenByDescending(t => (int)t.Priority)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt);
    }
}
