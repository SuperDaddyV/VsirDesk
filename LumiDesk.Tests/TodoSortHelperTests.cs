using LumiDesk.Helpers;
using LumiDesk.Models;

namespace LumiDesk.Tests;

public class TodoSortHelperTests
{
    [Fact]
    public void Sort_PriorityFirst_ThenNearestDueDate()
    {
        var todos = new[]
        {
            new TodoItem { Id = 1, Title = "Low Later", Priority = TodoPriority.Low, DueDate = DateTime.Today.AddDays(5) },
            new TodoItem { Id = 2, Title = "High Later", Priority = TodoPriority.High, DueDate = DateTime.Today.AddDays(10) },
            new TodoItem { Id = 3, Title = "High Soon", Priority = TodoPriority.High, DueDate = DateTime.Today.AddDays(1) },
            new TodoItem { Id = 4, Title = "Medium", Priority = TodoPriority.Medium, DueDate = DateTime.Today.AddDays(2) }
        };

        var sorted = TodoSortHelper.Sort(todos).Select(t => t.Id).ToList();

        Assert.Equal([3, 2, 4, 1], sorted);
    }

    [Fact]
    public void Sort_CompletedItems_AfterIncomplete()
    {
        var todos = new[]
        {
            new TodoItem { Id = 1, Title = "Done High", Priority = TodoPriority.High, IsCompleted = true, DueDate = DateTime.Today },
            new TodoItem { Id = 2, Title = "Open Low", Priority = TodoPriority.Low, DueDate = DateTime.Today.AddDays(3) }
        };

        var sorted = TodoSortHelper.Sort(todos).Select(t => t.Id).ToList();

        Assert.Equal([2, 1], sorted);
    }
}
