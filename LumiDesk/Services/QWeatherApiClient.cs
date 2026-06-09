using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using LumiDesk.Helpers;

namespace LumiDesk.Services;

/// <summary>
/// 和风天气 HTTP 客户端：支持个人 API Host + X-QW-Api-Key，并兼容旧版公共域名。
/// </summary>
public class QWeatherApiClient : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly HttpClient _httpClient;

    public QWeatherApiClient(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public string GetUserApiKey() => _settingsService.GetValue("WeatherApiKey", "").Trim();

    public string GetUserApiHost() => NormalizeHost(_settingsService.GetValue("WeatherApiHost", ""));

    public bool IsUsingBuiltInDefaults =>
        string.IsNullOrEmpty(GetUserApiKey()) && string.IsNullOrEmpty(GetUserApiHost());

    public string GetApiKey()
    {
        var userKey = GetUserApiKey();
        return !string.IsNullOrEmpty(userKey)
            ? userKey
            : WeatherApiDefaults.GetDefaultApiKey(_settingsService);
    }

    public string GetApiHost()
    {
        var userHost = GetUserApiHost();
        return !string.IsNullOrEmpty(userHost)
            ? userHost
            : WeatherApiDefaults.GetDefaultApiHost(_settingsService);
    }

    public async Task<string?> GetAsync(
        string pathOnCustomHost,
        string query,
        CancellationToken cancellationToken = default,
        string? legacyHost = null,
        string? legacyPath = null)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        var host = GetApiHost();
        if (!string.IsNullOrEmpty(host))
        {
            var customResult = await SendAsync(host, pathOnCustomHost, query, apiKey, cancellationToken);
            if (customResult != null)
            {
                return customResult;
            }
        }

        if (!string.IsNullOrEmpty(legacyHost) && !string.IsNullOrEmpty(legacyPath))
        {
            return await SendAsync(legacyHost, legacyPath, query, apiKey, cancellationToken, allowQueryKeyFallback: true);
        }

        return null;
    }

    public async Task<QWeatherValidationResult> ValidateAsync(
        string? apiKey = null,
        string? apiHost = null,
        CancellationToken cancellationToken = default)
    {
        apiKey = (apiKey ?? GetApiKey()).Trim();
        apiHost = NormalizeHost(apiHost ?? GetApiHost());

        if (string.IsNullOrEmpty(apiKey))
        {
            return QWeatherValidationResult.Fail("API Key 为空");
        }

        if (!string.IsNullOrEmpty(apiHost))
        {
            var response = await SendAsync(apiHost, "/v7/weather/now", "location=101010100", apiKey, cancellationToken);
            var code = ParseCode(response);
            if (code == "200")
            {
                return QWeatherValidationResult.Ok();
            }

            if (code == "401" || code == "403")
            {
                return QWeatherValidationResult.Fail(
                    "API Key 与 API Host 不匹配或凭据无效。请确认控制台中 Key 与 Host 属于同一项目。");
            }

            if (!string.IsNullOrEmpty(code))
            {
                return QWeatherValidationResult.Fail($"校验失败（和风错误码 {code}）");
            }
        }

        var legacyResponse = await SendAsync(
            "devapi.qweather.com",
            "/v7/weather/now",
            "location=101010100",
            apiKey,
            cancellationToken,
            allowQueryKeyFallback: true);

        var legacyCode = ParseCode(legacyResponse);
        if (legacyCode == "200")
        {
            return QWeatherValidationResult.Ok();
        }

        if (string.IsNullOrEmpty(apiHost))
        {
            return QWeatherValidationResult.Fail(
                "校验失败：请在设置中填写 API Host（登录和风控制台 → 设置，形如 xxx.qweatherapi.com），与 API Key 配套使用。");
        }

        return QWeatherValidationResult.Fail(
            string.IsNullOrEmpty(legacyCode)
                ? "无法连接和风天气服务，请检查网络或 API Host 是否正确。"
                : $"校验失败（和风错误码 {legacyCode}）");
    }

    private async Task<string?> SendAsync(
        string host,
        string path,
        string query,
        string apiKey,
        CancellationToken cancellationToken,
        bool allowQueryKeyFallback = false)
    {
        var pathPart = path.StartsWith('/') ? path : "/" + path;
        var queryPart = string.IsNullOrEmpty(query) ? "" : (query.StartsWith('?') ? query : "?" + query);
        var url = $"https://{host}{pathPart}{queryPart}";

        var headerResult = await SendRequestAsync(url, apiKey, useHeaderAuth: true, cancellationToken);
        if (ParseCode(headerResult) == "200")
        {
            return headerResult;
        }

        var urlWithKey = queryPart.Contains("key=")
            ? url
            : $"{url}{(queryPart.Contains('?') ? "&" : "?")}key={Uri.EscapeDataString(apiKey)}";
        var queryResult = await SendRequestAsync(urlWithKey, apiKey, useHeaderAuth: false, cancellationToken);
        if (ParseCode(queryResult) == "200")
        {
            return queryResult;
        }

        return queryResult ?? headerResult;
    }

    private async Task<string?> SendRequestAsync(
        string url,
        string apiKey,
        bool useHeaderAuth,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (useHeaderAuth)
            {
                request.Headers.TryAddWithoutValidation("X-QW-Api-Key", apiKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return body;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string? ParseCode(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var doc = JsonSerializer.Deserialize<QWeatherCodeResponse>(json);
            return doc?.Code;
        }
        catch
        {
            return null;
        }
    }

    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        host = host.Trim();
        if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            host = host[8..];
        }
        else if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            host = host[7..];
        }

        var slash = host.IndexOf('/');
        if (slash >= 0)
        {
            host = host[..slash];
        }

        return host.TrimEnd('/');
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private class QWeatherCodeResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}

public readonly struct QWeatherValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; }

    public static QWeatherValidationResult Ok() => new() { IsValid = true, Message = string.Empty };

    public static QWeatherValidationResult Fail(string message) => new() { IsValid = false, Message = message };
}
