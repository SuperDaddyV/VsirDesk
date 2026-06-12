using System.IO;
using System.Diagnostics;
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
        var name = GetDisplayName(path);

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

    public static bool IsSupportedPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return File.Exists(path) || Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    private static string GetDisplayName(string path)
    {
        if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            var shortcutName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(shortcutName))
            {
                return shortcutName;
            }

            var resolvedName = ShellLinkHelper.TryGetShortcutDisplayName(path);
            if (!string.IsNullOrWhiteSpace(resolvedName))
            {
                return resolvedName;
            }
        }

        if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                var name = versionInfo.FileDescription;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = versionInfo.ProductName;
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name.Trim();
                }
            }
            catch
            {
            }
        }

        var fallback = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return Path.GetFileName(path);
    }
}
