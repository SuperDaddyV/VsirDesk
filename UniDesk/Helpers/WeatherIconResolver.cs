using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace UniDesk.Helpers;

/// <summary>
/// 解析天气图标：按和风天气图标代码加载本地 QWeather SVG。
/// </summary>
public static partial class WeatherIconResolver
{
    private const string FallbackCode = "999";

    private static readonly string[] QWeatherSvgFolders =
    [
        Path.Combine("icon", "QWeather-Icons-1.8.0", "icons"),
        Path.Combine("Assets", "Weather", "QWeather-Icons")
    ];

    private static readonly string[] QWeatherCssFiles =
    [
        Path.Combine("icon", "QWeather-Icons-1.8.0", "font", "qweather-icons.css"),
        Path.Combine("Assets", "Weather", "qweather-icons.css")
    ];

    private static readonly Lazy<IReadOnlyDictionary<string, char>> GlyphMap = new(LoadGlyphMap);
    private static readonly Dictionary<string, ImageSource> SvgCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock SvgCacheLock = new();

    public static WeatherIconDisplay Resolve(string? iconCode, string? weatherDescription)
    {
        var code = NormalizeIconCode(iconCode, weatherDescription);
        var palette = GetPalette(code);
        var image = ResolveQWeatherSvg(code, palette)
            ?? ResolveQWeatherSvg(FallbackCode, GetPalette(FallbackCode));
        if (image != null)
        {
            return new WeatherIconDisplay
            {
                UseImage = true,
                ImageSource = image,
                Glyph = string.Empty,
                GlyphForeground = Brushes.Transparent
            };
        }

        var glyph = ResolveQWeatherGlyph(code);
        return new WeatherIconDisplay
        {
            UseImage = false,
            ImageSource = null,
            Glyph = glyph,
            GlyphForeground = GetGlyphBrush(code)
        };
    }

    public static string NormalizeIconCode(string? iconCode, string? weatherDescription)
    {
        if (!string.IsNullOrWhiteSpace(iconCode))
        {
            return iconCode.Trim();
        }

        if (string.IsNullOrWhiteSpace(weatherDescription))
        {
            return "999";
        }

        return weatherDescription switch
        {
            _ when weatherDescription.Contains('晴') && !weatherDescription.Contains('云') => "100",
            _ when weatherDescription.Contains('阴') => "104",
            _ when weatherDescription.Contains('雷') => "302",
            _ when weatherDescription.Contains('雪') => "400",
            _ when weatherDescription.Contains('雨') => "305",
            _ when weatherDescription.Contains('雾') || weatherDescription.Contains('霾') => "501",
            _ when weatherDescription.Contains('云') => "101",
            _ => "999"
        };
    }

    private static ImageSource? ResolveQWeatherSvg(string code, WeatherIconPalette palette)
    {
        var path = ResolveQWeatherSvgPath(code);
        if (path == null)
        {
            return null;
        }

        var cacheKey = $"{path}|{palette.Fill}|{palette.Stroke}|{palette.Accent}";
        lock (SvgCacheLock)
        {
            if (SvgCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
        }

        try
        {
            var document = XDocument.Load(path);
            var root = document.Root;
            if (root == null)
            {
                return null;
            }

            var viewBox = ParseViewBox(root.Attribute("viewBox")?.Value);
            var group = new DrawingGroup();
            group.ClipGeometry = new RectangleGeometry(viewBox);

            var paths = root
                .Descendants()
                .Where(element => element.Name.LocalName == "path")
                .Select(element => element.Attribute("d")?.Value)
                .Where(data => !string.IsNullOrWhiteSpace(data))
                .ToList();

            for (var i = 0; i < paths.Count; i++)
            {
                var geometry = Geometry.Parse(paths[i]!);
                geometry.Freeze();

                var brush = CreateBrush(i == 0 ? palette.Fill : palette.Accent);
                var pen = new Pen(CreateBrush(palette.Stroke), palette.StrokeThickness);
                pen.Freeze();
                group.Children.Add(new GeometryDrawing(brush, pen, geometry));
            }

            group.Freeze();
            var image = new DrawingImage(group);
            image.Freeze();

            lock (SvgCacheLock)
            {
                SvgCache[cacheKey] = image;
            }

            return image;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveQWeatherSvgPath(string code)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        foreach (var folder in QWeatherSvgFolders)
        {
            var iconDir = Path.Combine(baseDir, folder);
            var filledPath = Path.Combine(iconDir, $"{code}-fill.svg");
            if (File.Exists(filledPath))
            {
                return filledPath;
            }

            var outlinePath = Path.Combine(iconDir, $"{code}.svg");
            if (File.Exists(outlinePath))
            {
                return outlinePath;
            }
        }

        return null;
    }

    private static Rect ParseViewBox(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var parts = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4
                && double.TryParse(parts[0], out var x)
                && double.TryParse(parts[1], out var y)
                && double.TryParse(parts[2], out var width)
                && double.TryParse(parts[3], out var height))
            {
                return new Rect(x, y, width, height);
            }
        }

        return new Rect(0, 0, 16, 16);
    }

    private static string ResolveQWeatherGlyph(string code)
    {
        if (GlyphMap.Value.TryGetValue(code, out var glyph))
        {
            return glyph.ToString();
        }

        if (GlyphMap.Value.TryGetValue("999", out var fallback))
        {
            return fallback.ToString();
        }

        return "\uf12d";
    }

    private static Brush GetGlyphBrush(string code)
    {
        var brush = CreateBrush(GetPalette(code).Fill);
        brush.Freeze();
        return brush;
    }

    private static WeatherIconPalette GetPalette(string code)
    {
        if (!int.TryParse(code, out var numeric))
        {
            return new WeatherIconPalette("#D4E4EB", "#EDF8FC", "#A9C1CC", 0.26);
        }

        return numeric switch
        {
            100 => new WeatherIconPalette("#FFD66B", "#FFF2B6", "#FFB84D", 0.20),
            >= 101 and <= 103 => new WeatherIconPalette("#D7E6EC", "#F3FAFD", "#AFC7D1", 0.24),
            104 => new WeatherIconPalette("#C7D8E1", "#EAF4F8", "#9FB5C0", 0.24),
            >= 150 and <= 153 => new WeatherIconPalette("#A8B4FF", "#DDE4FF", "#6D79C8", 0.22),
            >= 300 and <= 399 => new WeatherIconPalette("#7BC8F6", "#D7F3FF", "#3FA6D9", 0.22),
            >= 400 and <= 499 => new WeatherIconPalette("#DFF8FF", "#FFFFFF", "#9DD7E7", 0.22),
            >= 500 and <= 515 => new WeatherIconPalette("#C9D1D4", "#F0F5F6", "#98A7AC", 0.24),
            >= 600 and <= 699 => new WeatherIconPalette("#C9D1D4", "#F0F5F6", "#98A7AC", 0.24),
            900 => new WeatherIconPalette("#FFB36F", "#FFE2BF", "#F07B47", 0.22),
            901 => new WeatherIconPalette("#77D5FF", "#D9F5FF", "#32A7D6", 0.22),
            _ => new WeatherIconPalette("#D4E4EB", "#EDF8FC", "#A9C1CC", 0.26)
        };
    }

    private static SolidColorBrush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static IReadOnlyDictionary<string, char> LoadGlyphMap()
    {
        var map = new Dictionary<string, char>();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var cssPath = QWeatherCssFiles
            .Select(file => Path.Combine(baseDir, file))
            .FirstOrDefault(File.Exists);
        if (cssPath == null)
        {
            return map;
        }

        foreach (var line in File.ReadAllLines(cssPath))
        {
            var match = QiGlyphRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var code = match.Groups[1].Value;
            if (code.Contains('-', StringComparison.Ordinal))
            {
                continue;
            }

            var hex = match.Groups[2].Value;
            map[code] = (char)Convert.ToInt32(hex, 16);
        }

        return map;
    }

    [GeneratedRegex(@"\.qi-(\d+)::before\s*\{\s*content:\s*""\\([0-9a-fA-F]+)""")]
    private static partial Regex QiGlyphRegex();
}

internal sealed record WeatherIconPalette(string Fill, string Stroke, string Accent, double StrokeThickness);

public sealed class WeatherIconDisplay
{
    public bool UseImage { get; init; }
    public ImageSource? ImageSource { get; init; }
    public string Glyph { get; init; } = string.Empty;
    public Brush GlyphForeground { get; init; } = Brushes.White;
}
