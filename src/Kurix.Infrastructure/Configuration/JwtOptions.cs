namespace Kurix.Infrastructure.Configuration;

/// <summary>
/// Binds the <c>Jwt</c> section used to issue and validate dashboard tokens. The
/// signing key is a secret (User Secrets / Key Vault).
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "kurix";
    public string Audience { get; set; } = "kurix-dashboard";
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Token lifetime in hours.</summary>
    public int ExpiryHours { get; set; } = 8;
}
