namespace UniDesk.Models;

public class ClipboardHistoryItem
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public int UseCount { get; set; }

    public string DisplayTitle => BuildDisplayText(Content);

    public string Summary => BuildDisplayText(Content);

    public static string BuildDisplayText(string? content)
    {
        var text = string.Join(" ", (content ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line)));

        return string.IsNullOrWhiteSpace(text) ? "空文本" : text;
    }
}
