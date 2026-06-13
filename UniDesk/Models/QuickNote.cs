using System.Globalization;

namespace UniDesk.Models;

public class QuickNote
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title))
            {
                return Title.Trim();
            }

            var firstLine = Content
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

            return string.IsNullOrWhiteSpace(firstLine) ? "未命名便签" : firstLine;
        }
    }

    public string Summary
    {
        get
        {
            var text = string.IsNullOrWhiteSpace(Content)
                ? "空白便签"
                : string.Join(" ", Content
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line)));

            return string.IsNullOrWhiteSpace(text) ? "空白便签" : text;
        }
    }

    public string UpdatedAtText
    {
        get
        {
            var updatedAt = UpdatedAt == default ? CreatedAt : UpdatedAt;
            if (updatedAt == default)
            {
                return string.Empty;
            }

            return updatedAt.ToLocalTime().ToString("M/d HH:mm", CultureInfo.CurrentCulture);
        }
    }
}
