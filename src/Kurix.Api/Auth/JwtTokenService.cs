using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kurix.Core.Entities;
using Kurix.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kurix.Api.Auth;

/// <summary>Issues signed JWTs for authenticated dashboard users.</summary>
public interface IJwtTokenService
{
    string CreateToken(DashboardUser user);
}

/// <summary>
/// HMAC-SHA256 JWT issuer. Tokens carry the user id, tenant id, email and role so
/// dashboard endpoints can scope every query to the caller's tenant.
/// </summary>
public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    /// <summary>Claim type carrying the tenant id.</summary>
    public const string TenantClaim = "tenant_id";

    private readonly JwtOptions _options = options.Value;

    public string CreateToken(DashboardUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(TenantClaim, user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.ExpiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
