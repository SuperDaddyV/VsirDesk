namespace UniDesk.Models;

public class NoteItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
