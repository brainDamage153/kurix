using Kurix.Core.Enums;

namespace Kurix.Core.Entities;

/// <summary>
/// A business (PYME) served by Kurix. The root of the multi-tenant model:
/// every other tenant-owned entity references <see cref="Id"/>, and every
/// data query — SQL and Azure AI Search — must be filtered by it.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name of the business.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Hash of the widget API key. The plaintext key is shown to the tenant
    /// once at creation and never stored.
    /// </summary>
    public required string ApiKeyHash { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>
    /// Free-form JSON with tenant configuration: bot persona, enabled tools,
    /// escalation webhook, etc. Parsed into a strongly-typed settings model
    /// by the application layer.
    /// </summary>
    public string SettingsJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<KnowledgeDocument> KnowledgeDocuments { get; set; } = new List<KnowledgeDocument>();
    public ICollection<DashboardUser> DashboardUsers { get; set; } = new List<DashboardUser>();
}
