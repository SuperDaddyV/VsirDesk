using System.Text.Json;
using LumiDesk.Models;

namespace LumiDesk.Services;

public class LayoutService : ILayoutService
{
    private static readonly string[] ModuleOrder = ["ClockWeather", "Shortcuts", "Todos", "Notes"];

    private static readonly Dictionary<string, double> DefaultHeights = new()
    {
        ["ClockWeather"] = 150,
        ["Shortcuts"] = 110,
        ["Todos"] = 180,
        ["Notes"] = 200
    };

    private readonly ISettingsService _settingsService;

    public LayoutService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public string GetDefaultLayout()
    {
        return JsonSerializer.Serialize(CreateDefaultLayout());
    }

    private static List<WidgetLayout> CreateDefaultLayout()
    {
        return ModuleOrder.Select((key, index) => new WidgetLayout
        {
            WidgetKey = key,
            Order = index,
            Height = DefaultHeights[key],
            IsLocked = true
        }).ToList();
    }

    public string SerializeLayout(List<WidgetLayout> layout)
    {
        return JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
    }

    public List<WidgetLayout>? DeserializeLayout(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var layout = JsonSerializer.Deserialize<List<WidgetLayout>>(json);
            if (layout == null || layout.Count == 0)
            {
                return null;
            }

            return MigrateLayout(layout);
        }
        catch
        {
            return null;
        }
    }

    public void SaveLayout(string layout)
    {
        _settingsService.SetValue("WidgetLayout", layout);
    }

    public string? LoadLayout()
    {
        return _settingsService.GetValue("WidgetLayout", "");
    }

    public void ResetToDefault()
    {
        SaveLayout(GetDefaultLayout());
    }

    public List<WidgetLayout> LoadOrGetDefault()
    {
        var layoutJson = LoadLayout();
        var layout = DeserializeLayout(layoutJson ?? "");

        if (layout == null || !IsValidLayout(layout))
        {
            layout = CreateDefaultLayout();
            SaveLayout(SerializeLayout(layout));
        }

        return layout.OrderBy(w => w.Order).ToList();
    }

    private static List<WidgetLayout> MigrateLayout(List<WidgetLayout> layout)
    {
        if (layout.Any(w => w.WidgetKey == "ClockWeather"))
        {
            return NormalizeOrder(layout);
        }

        if (layout.Any(w => w.WidgetKey is "Clock" or "Weather"))
        {
            return CreateDefaultLayout();
        }

        return NormalizeOrder(layout);
    }

    private static List<WidgetLayout> NormalizeOrder(List<WidgetLayout> layout)
    {
        var result = new List<WidgetLayout>();
        var order = 0;
        foreach (var key in ModuleOrder)
        {
            var existing = layout.FirstOrDefault(w => w.WidgetKey == key);
            result.Add(existing ?? new WidgetLayout
            {
                WidgetKey = key,
                Order = order,
                Height = DefaultHeights[key],
                IsLocked = true
            });
            result[^1].Order = order++;
        }

        return result;
    }

    private static bool IsValidLayout(List<WidgetLayout> layout)
    {
        if (layout.Count != ModuleOrder.Length)
        {
            return false;
        }

        var keys = layout.Select(w => w.WidgetKey).ToHashSet();
        return ModuleOrder.All(keys.Contains);
    }
}
