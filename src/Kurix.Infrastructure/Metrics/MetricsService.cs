using Kurix.Core.Enums;
using Kurix.Core.Metrics;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Infrastructure.Metrics;

/// <summary>
/// Computes tenant-scoped dashboard metrics directly from the relational store.
/// Cost is estimated from token usage at gpt-4o-mini list prices.
/// </summary>
public class MetricsService(KurixDbContext db) : IMetricsService
{
    // gpt-4o-mini list prices (USD per 1M tokens). Kept as constants; revisit if pricing changes.
    private const decimal InputUsdPerMillion = 0.15m;
    private const decimal OutputUsdPerMillion = 0.60m;
    private const int TopQuestions = 5;

    public async Task<MetricsSummary> GetSummaryAsync(Guid tenantId, CancellationToken ct = default)
    {
        var conversations = db.Conversations.Where(c => c.TenantId == tenantId);

        var total = await conversations.CountAsync(ct);
        var escalated = await conversations.CountAsync(c => c.Status == ConversationStatus.Escalated, ct);

        // Materialize the tenant's conversation ids, then aggregate messages by id.
        // Avoids a correlated subquery that some providers can't translate.
        var conversationIds = await conversations.Select(c => c.Id).ToListAsync(ct);
        var tenantMessages = db.Messages.Where(m => conversationIds.Contains(m.ConversationId));

        var tokensIn = await tenantMessages.SumAsync(m => (long)m.TokensIn, ct);
        var tokensOut = await tenantMessages.SumAsync(m => (long)m.TokensOut, ct);

        // Group user questions in memory: portable across providers and cheap at
        // dashboard volumes (only the user-message texts are materialized).
        var userQuestions = await tenantMessages
            .Where(m => m.Role == MessageRole.User)
            .Select(m => m.Content)
            .ToListAsync(ct);

        var frequent = userQuestions
            .GroupBy(content => content)
            .Select(g => new FrequentQuestion(g.Key, g.Count()))
            .OrderByDescending(q => q.Count)
            .Take(TopQuestions)
            .ToList();

        var escalationRate = total == 0 ? 0d : (double)escalated / total;
        var cost = tokensIn / 1_000_000m * InputUsdPerMillion +
                   tokensOut / 1_000_000m * OutputUsdPerMillion;

        return new MetricsSummary(
            total, escalated, escalationRate, tokensIn, tokensOut,
            decimal.Round(cost, 4), frequent);
    }
}
