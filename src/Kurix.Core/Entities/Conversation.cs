using Kurix.Core.Enums;

namespace Kurix.Core.Entities;

/// <summary>
/// A chat session between an end user (via the widget) and the AI engine,
/// owned by a single tenant.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>
    /// Opaque session identifier issued to the widget, used to correlate
    /// successive messages from the same browser session.
    /// </summary>
    public required string SessionId { get; set; }

    public ConversationStatus Status { get; set; } = ConversationStatus.Active;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastMessageAt { get; set; }

    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
