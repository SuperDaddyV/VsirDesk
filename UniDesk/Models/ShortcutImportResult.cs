namespace UniDesk.Models;

public sealed class ShortcutImportResult
{
    public int AddedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int InvalidCount { get; set; }
    public int LimitSkippedCount { get; set; }

    public bool HasChanges => AddedCount > 0;

    public string ToUserMessage()
    {
        var parts = new List<string>();
        if (AddedCount > 0)
        {
            parts.Add($"已添加 {AddedCount} 个快捷方式");
        }

        if (DuplicateCount > 0)
        {
            parts.Add($"跳过 {DuplicateCount} 个重复项");
        }

        if (InvalidCount > 0)
        {
            parts.Add($"忽略 {InvalidCount} 个无效项");
        }

        if (LimitSkippedCount > 0)
        {
            parts.Add($"因数量限制跳过 {LimitSkippedCount} 个");
        }

        return parts.Count == 0 ? "没有可添加的快捷方式" : string.Join("，", parts);
    }
}
