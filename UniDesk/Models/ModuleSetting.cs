namespace UniDesk.Models;

public sealed class ModuleSetting
{
    public string ModuleId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    public ModuleSetting Clone() => new()
    {
        ModuleId = ModuleId,
        DisplayName = DisplayName,
        IsEnabled = IsEnabled,
        SortOrder = SortOrder
    };
}

public static class DashboardModuleIds
{
    public const string TimeWeather = "TimeWeather";
    public const string HardwareMonitor = "HardwareMonitor";
    public const string Shortcuts = "Shortcuts";
    public const string Todos = "Todos";
}

public static class DashboardModuleCatalog
{
    public const string SettingsKey = "ModuleSettings";

    public static IReadOnlyList<ModuleSetting> DefaultModules { get; } =
    [
        new()
        {
            ModuleId = DashboardModuleIds.TimeWeather,
            DisplayName = "时间天气",
            IsEnabled = true,
            SortOrder = 0
        },
        new()
        {
            ModuleId = DashboardModuleIds.HardwareMonitor,
            DisplayName = "硬件监视",
            IsEnabled = true,
            SortOrder = 1
        },
        new()
        {
            ModuleId = DashboardModuleIds.Shortcuts,
            DisplayName = "快捷方式",
            IsEnabled = true,
            SortOrder = 2
        },
        new()
        {
            ModuleId = DashboardModuleIds.Todos,
            DisplayName = "待办事项",
            IsEnabled = true,
            SortOrder = 3
        }
    ];

    public static IReadOnlySet<string> KnownModuleIds { get; } =
        DefaultModules.Select(module => module.ModuleId).ToHashSet(StringComparer.Ordinal);

    public static List<ModuleSetting> CreateDefaultModules() =>
        DefaultModules.Select(module => module.Clone()).ToList();

    public static string GetDisplayName(string moduleId) =>
        DefaultModules.FirstOrDefault(module => module.ModuleId == moduleId)?.DisplayName ?? moduleId;

    public static List<ModuleSetting> Normalize(IEnumerable<ModuleSetting>? modules)
    {
        var incoming = modules?
            .Where(module => !string.IsNullOrWhiteSpace(module.ModuleId))
            .GroupBy(module => module.ModuleId.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal)
            ?? new Dictionary<string, ModuleSetting>(StringComparer.Ordinal);

        var normalized = new List<ModuleSetting>();
        foreach (var defaultModule in DefaultModules)
        {
            if (incoming.TryGetValue(defaultModule.ModuleId, out var module))
            {
                normalized.Add(new ModuleSetting
                {
                    ModuleId = defaultModule.ModuleId,
                    DisplayName = defaultModule.DisplayName,
                    IsEnabled = module.IsEnabled,
                    SortOrder = module.SortOrder
                });
            }
            else
            {
                normalized.Add(defaultModule.Clone());
            }
        }

        var nextOrder = normalized.Count;
        foreach (var unknown in incoming.Values
                     .Where(module => !KnownModuleIds.Contains(module.ModuleId))
                     .OrderBy(module => module.SortOrder))
        {
            normalized.Add(new ModuleSetting
            {
                ModuleId = unknown.ModuleId.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(unknown.DisplayName)
                    ? unknown.ModuleId.Trim()
                    : unknown.DisplayName.Trim(),
                IsEnabled = unknown.IsEnabled,
                SortOrder = nextOrder++
            });
        }

        var ordered = normalized
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => GetDefaultOrder(module.ModuleId))
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SortOrder = i;
        }

        return ordered;
    }

    private static int GetDefaultOrder(string moduleId)
    {
        for (var i = 0; i < DefaultModules.Count; i++)
        {
            if (DefaultModules[i].ModuleId == moduleId)
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}
