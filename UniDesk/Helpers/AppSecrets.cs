using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniDesk.Helpers;

/// <summary>
/// secrets.json 中的可选凭据（不纳入版本控制）。
/// </summary>
internal static class AppSecrets
{
    private static readonly Lazy<SecretsRoot> Root = new(Load);

    public static string? AmapApiKey
    {
        get
        {
            var encrypted = Root.Value.AmapApiKeyEnc.Trim();
            if (string.IsNullOrEmpty(encrypted))
            {
                return null;
            }

            var plain = TryDecrypt(encrypted);
            return string.IsNullOrEmpty(plain) ? null : plain;
        }
    }

    private static SecretsRoot Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "secrets.json");
            if (!File.Exists(path))
            {
                return new SecretsRoot();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SecretsRoot>(json) ?? new SecretsRoot();
        }
        catch
        {
            return new SecretsRoot();
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

    private class SecretsRoot
    {
        [JsonPropertyName("AmapApiKeyEnc")]
        public string AmapApiKeyEnc { get; set; } = "";
    }
}
