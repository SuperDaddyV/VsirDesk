using System.Security.Cryptography;
using System.Text;

namespace LumiDesk.Helpers;

/// <summary>
/// 应用内置配置的加解密（AES-256-CBC，IV 前置），用于保护默认凭据等非明文存储。
/// </summary>
internal static class ConfigSecretProtector
{
    private static byte[] DeriveKey()
    {
        var seed = Encoding.UTF8.GetBytes("LumiDesk" + "QWeather" + "Default" + "v1");
        return SHA256.HashData(seed);
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = DeriveKey();
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plain = Encoding.UTF8.GetBytes(plainText);
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

        var combined = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, combined, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    public static string Decrypt(string cipherTextBase64)
    {
        if (string.IsNullOrWhiteSpace(cipherTextBase64))
        {
            return string.Empty;
        }

        var combined = Convert.FromBase64String(cipherTextBase64);
        if (combined.Length < 17)
        {
            return string.Empty;
        }

        var iv = new byte[16];
        Buffer.BlockCopy(combined, 0, iv, 0, 16);
        var cipher = new byte[combined.Length - 16];
        Buffer.BlockCopy(combined, 16, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = DeriveKey();
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
