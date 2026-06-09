using LumiDesk.Models;

namespace LumiDesk.Services;

public interface IWeatherService
{
    Task<WeatherInfo?> GetWeatherAsync(
        string city,
        CancellationToken cancellationToken = default,
        bool notifyUser = true);

    Task<WeatherInfo?> GetCachedWeatherAsync();

    Task<WeatherInfo?> RefreshWeatherAsync(
        CancellationToken cancellationToken = default,
        bool notifyUser = true);

    void CancelRefresh();

    Task SetCityAsync(string city);

    Task<QWeatherValidationResult> ValidateApiKeyAsync(
        string apiKey,
        string? apiHost = null,
        CancellationToken cancellationToken = default);

    string GetEffectiveApiKey();
}
