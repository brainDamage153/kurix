using System.Security.Claims;

namespace Kurix.Api.Auth;

/// <summary>Helpers to read Kurix claims off an authenticated dashboard user.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the tenant id carried in the JWT. Throws if missing/invalid — a
    /// signal that an endpoint requiring dashboard auth was reached without it.
    /// </summary>
    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtTokenService.TenantClaim);
        if (Guid.TryParse(value, out var tenantId))
            return tenantId;

        throw new InvalidOperationException("The authenticated user has no valid tenant claim.");
    }
}
