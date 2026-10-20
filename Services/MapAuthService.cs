using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MSLX.Plugin.MapRender.Services;

public class MapAuthService
{
    private static MapAuthService? _instance;
    public static MapAuthService Instance => _instance ??= new MapAuthService();

    private readonly object _keyLock = new();
    private byte[]? _secretKeyBytes;

    private byte[] GetOrCreateSecretKey()
    {
        if (_secretKeyBytes != null) return _secretKeyBytes;

        lock (_keyLock)
        {
            if (_secretKeyBytes != null) return _secretKeyBytes;

            try
            {
                var pluginConfig = SDK.MSLX.Config.GetPluginConfig("mslx-plugin-map-render");
                var keyStr = pluginConfig.ReadConfigKey("AuthSecretKey")?.ToString();
                if (string.IsNullOrWhiteSpace(keyStr))
                {
                    keyStr = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                    pluginConfig.WriteConfigKey("AuthSecretKey", keyStr);
                }
                _secretKeyBytes = Encoding.UTF8.GetBytes(keyStr);
            }
            catch
            {
                // 回退到内存临时密钥
                _secretKeyBytes = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
            }

            return _secretKeyBytes;
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static byte[]? Base64UrlDecode(string input)
    {
        try
        {
            string output = input.Replace("-", "+").Replace("_", "/");
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            return Convert.FromBase64String(output);
        }
        catch
        {
            return null;
        }
    }

    public string GenerateMapToken(uint serverId, TimeSpan validity)
    {
        var secret = GetOrCreateSecretKey();
        var exp = DateTimeOffset.UtcNow.Add(validity).ToUnixTimeSeconds();
        var payload = new
        {
            serverId,
            exp,
            nonce = Guid.NewGuid().ToString("N")
        };

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var payloadPart = Base64UrlEncode(payloadBytes);

        using var hmac = new HMACSHA256(secret);
        var signatureBytes = hmac.ComputeHash(payloadBytes);
        var signaturePart = Base64UrlEncode(signatureBytes);

        return $"{payloadPart}.{signaturePart}";
    }

    public bool ValidateMapToken(string? token, uint expectedServerId)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.');
        if (parts.Length != 2) return false;

        var payloadBytes = Base64UrlDecode(parts[0]);
        var signatureBytes = Base64UrlDecode(parts[1]);
        if (payloadBytes == null || signatureBytes == null) return false;

        var secret = GetOrCreateSecretKey();
        using var hmac = new HMACSHA256(secret);
        var expectedSignature = hmac.ComputeHash(payloadBytes);

        if (!CryptographicOperations.FixedTimeEquals(signatureBytes, expectedSignature))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(payloadBytes);
            var root = doc.RootElement;
            if (!root.TryGetProperty("serverId", out var sProp) || sProp.GetUInt32() != expectedServerId)
                return false;

            if (!root.TryGetProperty("exp", out var expProp) || expProp.GetInt64() < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool VerifyPasswordHash(string? inputHash, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(inputHash) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var inputBytes = Encoding.UTF8.GetBytes(inputHash.Trim().ToLowerInvariant());
        var storedBytes = Encoding.UTF8.GetBytes(storedHash.Trim().ToLowerInvariant());

        return CryptographicOperations.FixedTimeEquals(inputBytes, storedBytes);
    }
}
