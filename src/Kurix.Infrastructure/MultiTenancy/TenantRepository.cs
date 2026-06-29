using Kurix.Core.Entities;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Infrastructure.MultiTenancy;

/// <summary>
/// EF Core implementation of <see cref="ITenantRepository"/>. Tenant lookups are
/// read-only and tracked-free for performance; the resolved tenant is cached in
/// the request-scoped <see cref="ITenantContext"/>.
/// </summary>
public class TenantRepository(KurixDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken ct = default) =>
        db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.ApiKeyHash == apiKeyHash, ct);

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default) =>
        db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
}
