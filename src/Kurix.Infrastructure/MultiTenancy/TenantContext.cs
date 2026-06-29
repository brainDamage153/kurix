using Kurix.Core.Entities;
using Kurix.Core.MultiTenancy;

namespace Kurix.Infrastructure.MultiTenancy;

/// <summary>
/// Request-scoped implementation of <see cref="ITenantContext"/>. Registered as
/// <c>Scoped</c> so each request gets its own instance, set once by the
/// resolution middleware.
/// </summary>
public class TenantContext : ITenantContext
{
    private Tenant? _tenant;

    public bool IsResolved => _tenant is not null;

    public Tenant Tenant => _tenant
        ?? throw new InvalidOperationException(
            "No tenant has been resolved for the current request. " +
            "Ensure the request carries a valid API key and passes the tenant-resolution middleware.");

    public Guid TenantId => Tenant.Id;

    public void SetTenant(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (_tenant is not null)
            throw new InvalidOperationException("The tenant has already been resolved for this request.");
        _tenant = tenant;
    }
}
