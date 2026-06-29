using Kurix.Core.Enums;

namespace Kurix.Core.Entities;

/// <summary>
/// A user who can sign in to the dashboard for a given tenant. Authenticated
/// with email + password (hashed), authorized via JWT carrying the tenant id.
/// </summary>
public class DashboardUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public DashboardRole Role { get; set; } = DashboardRole.Member;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant? Tenant { get; set; }
}
