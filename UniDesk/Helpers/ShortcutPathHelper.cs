using System.IO;
using UniDesk.Models;

namespace UniDesk.Helpers;

/// <summary>
/// 根据用户选择的文件路径构建快捷方式项。
/// </summary>
public static class ShortcutPathHelper
{
    private static readonly HashSet<string> ApplicationExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".lnk", ".url", ".bat", ".cmd", ".com", ".msc", ".msi", ".ps1"
    };

    public static ShortcutItem CreateFromPath(string path, int sortOrder)
    {
        var name = ShellLinkHelper.TryGetShortcutDisplayName(path)
                   ?? Path.GetFileNameWithoutExtension(path);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileName(path);
        }

        var extension = Path.GetExtension(path);
        var isFolder = Directory.Exists(path);
        var type = isFolder
            ? ShortcutType.Folder
            : ApplicationExtensions.Contains(extension)
                ? ShortcutType.Application
                : ShortcutType.File;

        return new ShortcutItem
        {
            Name = name,
            Path = path,
            IconLookupPath = path,
            Type = type,
            SortOrder = sortOrder
        };
    }
}
