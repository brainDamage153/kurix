using Kurix.Core.Entities;

namespace Kurix.Core.MultiTenancy;

/// <summary>
/// Ambient, request-scoped accessor for the tenant resolved for the current
/// request. Populated once by the tenant-resolution middleware (from the widget
/// API key) and read everywhere downstream so that <b>every</b> data access —
/// SQL and Azure AI Search — can be filtered by the active tenant.
/// </summary>
public interface ITenantContext
{
    /// <summary>True once a tenant has been resolved for this request.</summary>
    bool IsResolved { get; }

    /// <summary>
    /// The resolved tenant. Throws if accessed before resolution; guard with
    /// <see cref="IsResolved"/> or use <see cref="TenantId"/> where appropriate.
    /// </summary>
    Tenant Tenant { get; }

    /// <summary>Convenience accessor for the resolved tenant's id.</summary>
    Guid TenantId { get; }

    /// <summary>
    /// Binds the tenant for the current request. Intended to be called exactly
    /// once, by the resolution middleware. Throws if already resolved.
    /// </summary>
    void SetTenant(Tenant tenant);
}
