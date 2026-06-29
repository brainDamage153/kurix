using Kurix.Core.Entities;

namespace Kurix.Core.Repositories;

/// <summary>
/// Tenant-scoped access to conversations. Every method takes a
/// <paramref name="tenantId"/> and filters by it — no cross-tenant reads.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Returns the active conversation for a widget session, creating one if none
    /// exists yet for the tenant + session.
    /// </summary>
    Task<Conversation> GetOrCreateBySessionAsync(
        Guid tenantId, string sessionId, CancellationToken ct = default);

    /// <summary>Loads a conversation with its messages, scoped to the tenant.</summary>
    Task<Conversation?> GetWithMessagesAsync(
        Guid tenantId, Guid conversationId, CancellationToken ct = default);

    /// <summary>Lists the tenant's most recent conversations (metadata only).</summary>
    Task<IReadOnlyList<Conversation>> ListAsync(
        Guid tenantId, int limit = 100, CancellationToken ct = default);
}
