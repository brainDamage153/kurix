namespace Kurix.Core.Auth;

/// <summary>
/// Hashes and verifies dashboard-user passwords. Unlike API keys, passwords are
/// low-entropy human secrets, so the implementation uses a salted, stretched
/// algorithm (PBKDF2) — never the deterministic API-key hasher.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string storedHash);
}
