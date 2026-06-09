using LumiDesk.Helpers;
using Xunit;

namespace LumiDesk.Tests;

public class WeatherIconResolverTests
{
    [Theory]
    [InlineData("104", "阴")]
    [InlineData("100", "晴")]
    [InlineData("305", "小雨")]
    public void Resolve_KnownCodes_UsesQWeatherSvgWhenAssetsPresent(string code, string desc)
    {
        var display = WeatherIconResolver.Resolve(code, desc);

        if (HasQWeatherSvgAssets())
        {
            Assert.True(display.UseImage);
            Assert.NotNull(display.ImageSource);
            return;
        }

        Assert.False(display.UseImage);
        Assert.False(string.IsNullOrEmpty(display.Glyph));
    }

    [Fact]
    public void Resolve_UnknownCode_FallsBackToQWeatherGlyph()
    {
        var display = WeatherIconResolver.Resolve("888", "未知");

        if (HasQWeatherSvgAssets())
        {
            Assert.True(display.UseImage);
            Assert.NotNull(display.ImageSource);
            return;
        }

        Assert.False(display.UseImage);
        Assert.False(string.IsNullOrEmpty(display.Glyph));
    }

    [Fact]
    public void NormalizeIconCode_FromDescription_MapsOvercast()
    {
        var code = WeatherIconResolver.NormalizeIconCode(null, "阴");
        Assert.Equal("104", code);
    }

    private static bool HasQWeatherSvgAssets()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var folders = new[]
        {
            Path.Combine(baseDir, "icon", "QWeather-Icons-1.8.0", "icons"),
            Path.Combine(baseDir, "Assets", "Weather", "QWeather-Icons")
        };
        return folders.Any(dir => Directory.Exists(dir) && Directory.GetFiles(dir, "*.svg").Length > 0);
    }
}
