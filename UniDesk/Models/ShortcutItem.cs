namespace UniDesk.Models;

public enum ShortcutType { Application, Folder, File }

public class ShortcutItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? LaunchArguments { get; set; }
    /// <summary>创建时用于提取图标，不持久化到数据库。</summary>
    public string? IconLookupPath { get; set; }
    /// <summary>创建时从程序目录 icon 文件夹复制的内置图标文件名，不持久化到数据库。</summary>
    public string? BundledIconFileName { get; set; }
    public ShortcutType Type { get; set; } = ShortcutType.Application;
    public string? IconPath { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
