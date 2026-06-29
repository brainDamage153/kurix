using System.Net.Http.Json;
using Kurix.Core.Enums;
using Kurix.Core.Escalation;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kurix.Infrastructure.Escalation;

/// <summary>
/// Flips a conversation to <see cref="ConversationStatus.Escalated"/> in SQL and
/// fires the tenant's configured escalation webhook (best-effort). Webhook
/// failures never break the conversation — they are logged and reported.
/// </summary>
public class EscalationService(
    KurixDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<EscalationService> logger) : IEscalationService
{
    public const string HttpClientName = "escalation";

    public async Task<EscalationResult> EscalateAsync(
        Guid tenantId, Guid conversationId, EscalationRequest request, CancellationToken ct = default)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId, ct);

        if (conversation is null)
            return new EscalationResult(false, "Conversación no encontrada.");

        conversation.Status = ConversationStatus.Escalated;
        await db.SaveChangesAsync(ct);

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        var settings = TenantSettings.Parse(tenant?.SettingsJson);

        var delivered = await TryFireWebhookAsync(settings.EscalationWebhookUrl, new
        {
            tenantId,
            conversationId,
            sessionId = conversation.SessionId,
            reason = request.Reason,
            summary = request.Summary,
            escalatedAt = DateTimeOffset.UtcNow
        }, ct);

        return new EscalationResult(
            delivered,
            "La conversación fue derivada a un agente humano.");
    }

    private async Task<bool> TryFireWebhookAsync(string? url, object payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var response = await client.PostAsJsonAsync(url, payload, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Escalation webhook returned {Status} for {Url}.",
                    (int)response.StatusCode, url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Escalation webhook call to {Url} failed.", url);
            return false;
        }
    }
}
