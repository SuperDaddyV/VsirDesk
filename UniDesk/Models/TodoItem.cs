namespace UniDesk.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
