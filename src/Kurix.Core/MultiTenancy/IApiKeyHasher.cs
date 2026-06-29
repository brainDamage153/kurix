namespace Kurix.Core.MultiTenancy;

/// <summary>
/// Hashes and generates widget API keys. Because the tenant is looked up by its
/// stored hash on every widget request, the hash must be <b>deterministic</b>
/// (unlike a password hash). API keys are high-entropy random values, so a fast
/// cryptographic digest is appropriate here.
/// </summary>
public interface IApiKeyHasher
{
    /// <summary>Produces the deterministic hash stored in <c>Tenant.ApiKeyHash</c>.</summary>
    string Hash(string apiKey);

    /// <summary>Constant-time comparison of a presented key against a stored hash.</summary>
    bool Verify(string apiKey, string storedHash);

    /// <summary>Generates a new random API key (plaintext, shown to the tenant once).</summary>
    string GenerateApiKey();
}
