using UniDesk.Models;

namespace UniDesk.Services;

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
