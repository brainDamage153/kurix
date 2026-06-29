using Kurix.Core.Entities;

namespace Kurix.Core.MultiTenancy;

/// <summary>
/// Tenant lookups needed to resolve and manage tenants. Implemented over EF Core
/// in the Infrastructure layer.
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Resolves a tenant by the deterministic hash of its API key. Returns
    /// <c>null</c> when no tenant matches.
    /// </summary>
    Task<Tenant?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken ct = default);

    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);
}
