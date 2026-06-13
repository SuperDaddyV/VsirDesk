namespace UniDesk.Models;

public class TextSnippet
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "默认";
    public bool IsPinned { get; set; }
    public int SortOrder { get; set; }
    public int UseCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title))
            {
                return Title.Trim();
            }

            return ClipboardHistoryItem.BuildDisplayText(Content);
        }
    }

    public string Summary => ClipboardHistoryItem.BuildDisplayText(Content);
}
