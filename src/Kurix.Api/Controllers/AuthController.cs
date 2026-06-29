using Kurix.Api.Auth;
using Kurix.Api.Contracts;
using Kurix.Core.Auth;
using Kurix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Api.Controllers;

/// <summary>Dashboard authentication: email + password in exchange for a JWT.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    KurixDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // Email is unique per tenant, so there may be multiple candidates across
        // tenants; verify the password against each and accept the first match.
        var candidates = await db.DashboardUsers
            .Where(u => u.Email == request.Email)
            .ToListAsync(ct);

        var user = candidates.FirstOrDefault(u => passwordHasher.Verify(request.Password, u.PasswordHash));
        if (user is null)
            return Unauthorized(new { error = "Credenciales inválidas." });

        var token = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, user.Email, user.Role.ToString(), user.TenantId));
    }
}
