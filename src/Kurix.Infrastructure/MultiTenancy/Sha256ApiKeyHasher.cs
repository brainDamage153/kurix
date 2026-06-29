using System.Security.Cryptography;
using System.Text;
using Kurix.Core.MultiTenancy;

namespace Kurix.Infrastructure.MultiTenancy;

/// <summary>
/// Deterministic SHA-256 hashing for widget API keys, so a tenant can be looked
/// up by hash in a single indexed query. API keys are 256 bits of CSPRNG output,
/// so a plain digest (no salt/stretching) is sufficient and necessary for the
/// lookup. Keys are prefixed with <c>kx_</c> for easy identification in logs/UIs.
/// </summary>
public class Sha256ApiKeyHasher : IApiKeyHasher
{
    private const string KeyPrefix = "kx_";

    public string Hash(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexStringLower(bytes);
    }

    public bool Verify(string apiKey, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var computed = Hash(apiKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(storedHash));
    }

    public string GenerateApiKey()
    {
        // 32 bytes -> 256 bits of entropy, URL-safe base64.
        var raw = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(raw)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return KeyPrefix + token;
    }
}
