using System.IO;
using UniDesk.Models;

namespace UniDesk.Helpers;

/// <summary>
/// 系统内置应用快捷方式目录。
/// </summary>
public sealed class SystemAppShortcut
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string? LaunchArguments { get; init; }
    /// <summary>用于提取图标的 Shell 路径或磁盘路径（可与启动路径不同）。</summary>
    public string? IconLookupPath { get; init; }
    /// <summary>icon 目录下的内置 PNG 文件名；设置后优先于 Shell 图标提取。</summary>
    public string? BundledIconFileName { get; init; }
    public ShortcutType Type { get; init; } = ShortcutType.Application;
}

public static class SystemAppCatalog
{
    private static readonly string SystemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);

    public static IReadOnlyList<SystemAppShortcut> Apps { get; } =
    [
        new()
        {
            Name = "计算器",
            Path = "calc.exe",
            IconLookupPath = Path.Combine(SystemDir, "calc.exe"),
            BundledIconFileName = "计算器.png"
        },
        new()
        {
            Name = "记事本",
            Path = "notepad.exe",
            IconLookupPath = Path.Combine(SystemDir, "notepad.exe"),
            BundledIconFileName = "记事本.png"
        },
        new()
        {
            Name = "画图",
            Path = "mspaint.exe",
            IconLookupPath = Path.Combine(SystemDir, "mspaint.exe"),
            BundledIconFileName = "画图.png"
        },
        new()
        {
            Name = "此电脑",
            Path = "shell:MyComputerFolder",
            Type = ShortcutType.Folder,
            IconLookupPath = "shell:MyComputerFolder",
            BundledIconFileName = "此电脑.png"
        },
        new()
        {
            Name = "文件管理",
            Path = "explorer.exe",
            IconLookupPath = Path.Combine(SystemDir, "explorer.exe"),
            BundledIconFileName = "007_文件管理.png"
        },
        new()
        {
            Name = "设置",
            Path = "ms-settings:",
            IconLookupPath = "ms-settings:",
            BundledIconFileName = "设置.png"
        }
    ];

    public static string? GetBundledIconPath(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "icon", fileName);
        return File.Exists(path) ? path : null;
    }
}
