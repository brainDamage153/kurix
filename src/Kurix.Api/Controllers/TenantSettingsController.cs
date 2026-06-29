using Kurix.Api.Auth;
using Kurix.Api.Contracts;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Api.Controllers;

/// <summary>Tenant configuration for the dashboard (JWT): bot persona and enabled tools.</summary>
[ApiController]
[Route("api/tenant/settings")]
[Authorize]
public class TenantSettingsController(KurixDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> Get(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return NotFound();

        var settings = TenantSettings.Parse(tenant.SettingsJson);
        return Ok(ToDto(settings));
    }

    [HttpPut]
    public async Task<ActionResult<TenantSettingsDto>> Update(
        [FromBody] TenantSettingsDto dto, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return NotFound();

        var settings = new TenantSettings
        {
            Persona = string.IsNullOrWhiteSpace(dto.Persona) ? new TenantSettings().Persona : dto.Persona,
            EnabledTools = dto.EnabledTools,
            EscalationWebhookUrl = dto.EscalationWebhookUrl,
            FallbackMessage = string.IsNullOrWhiteSpace(dto.FallbackMessage)
                ? new TenantSettings().FallbackMessage
                : dto.FallbackMessage
        };

        tenant.SettingsJson = settings.ToJson();
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(settings));
    }

    private static TenantSettingsDto ToDto(TenantSettings s) => new()
    {
        Persona = s.Persona,
        EnabledTools = s.EnabledTools,
        EscalationWebhookUrl = s.EscalationWebhookUrl,
        FallbackMessage = s.FallbackMessage
    };
}
