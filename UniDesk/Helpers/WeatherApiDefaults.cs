using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniDesk.Services;

namespace UniDesk.Helpers;

/// <summary>
/// 和风天气内置默认凭据（加密存储）。用户未配置 WeatherApiKey / WeatherApiHost 时启用。
/// 凭据优先从 secrets.json 读取，其次数据库，最后使用内置常量（保证安装包无 secrets.json 时仍可用）。
/// </summary>
internal static class WeatherApiDefaults
{
    internal const string BuiltInApiKeyEncrypted =
        "frezMQAzypvyQe+uLlNbjEl/I6tBXXfOjrrXocjSxl0q54zFSJIOJvYobLkAMgeisDIBmVPwfb2lYUKZforTaQ==";

    internal const string BuiltInApiHostEncrypted =
        "2WaLqNYbXge5eJBCj8h3hVh/MfarTTXf5wcVU+ExO5WeGYqs7ApkxwRVY9x9Tmf/";

    private static readonly Lazy<SecretsFile> Secrets = new(LoadSecrets);

    public const string DefaultApiKeySettingKey = "DefaultWeatherApiKeyEnc";
    public const string DefaultApiHostSettingKey = "DefaultWeatherApiHostEnc";

    public static string GetDefaultApiKey(ISettingsService settings)
    {
        var encrypted = ResolveEncryptedCredential(
            settings.GetValue(DefaultApiKeySettingKey, ""),
            Secrets.Value.ApiKeyEnc,
            BuiltInApiKeyEncrypted);

        return TryDecrypt(encrypted);
    }

    public static string GetDefaultApiHost(ISettingsService settings)
    {
        var encrypted = ResolveEncryptedCredential(
            settings.GetValue(DefaultApiHostSettingKey, ""),
            Secrets.Value.ApiHostEnc,
            BuiltInApiHostEncrypted);

        return QWeatherApiClient.NormalizeHost(TryDecrypt(encrypted));
    }

    private static string ResolveEncryptedCredential(string fromDatabase, string fromSecretsFile, string builtIn)
    {
        if (!string.IsNullOrWhiteSpace(fromDatabase))
        {
            return fromDatabase.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fromSecretsFile))
        {
            return fromSecretsFile.Trim();
        }

        return builtIn;
    }

    private static SecretsFile LoadSecrets()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "secrets.json");
            if (!File.Exists(path))
            {
                return new SecretsFile();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SecretsFile>(json) ?? new SecretsFile();
        }
        catch
        {
            return new SecretsFile();
        }
    }

    private static string TryDecrypt(string encrypted)
    {
        try
        {
            return ConfigSecretProtector.Decrypt(encrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    private class SecretsFile
    {
        [JsonPropertyName("WeatherApiKeyEnc")]
        public string ApiKeyEnc { get; set; } = "";

        [JsonPropertyName("WeatherApiHostEnc")]
        public string ApiHostEnc { get; set; } = "";
    }
}
