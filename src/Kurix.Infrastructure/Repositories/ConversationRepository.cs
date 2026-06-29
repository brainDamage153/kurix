using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.Repositories;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IConversationRepository"/>.</summary>
public class ConversationRepository(KurixDbContext db) : IConversationRepository
{
    public async Task<Conversation> GetOrCreateBySessionAsync(
        Guid tenantId, string sessionId, CancellationToken ct = default)
    {
        var existing = await db.Conversations
            .Where(c => c.TenantId == tenantId &&
                        c.SessionId == sessionId &&
                        c.Status != ConversationStatus.Closed)
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;

        var conversation = new Conversation { TenantId = tenantId, SessionId = sessionId };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }

    public Task<Conversation?> GetWithMessagesAsync(
        Guid tenantId, Guid conversationId, CancellationToken ct = default) =>
        db.Conversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<Conversation>> ListAsync(
        Guid tenantId, int limit = 100, CancellationToken ct = default) =>
        await db.Conversations
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.LastMessageAt ?? c.StartedAt)
            .Take(limit)
            .ToListAsync(ct);
}
