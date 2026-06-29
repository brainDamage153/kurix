namespace Kurix.Core.Escalation;

/// <summary>Context captured when a conversation is escalated to a human.</summary>
/// <param name="Reason">Short reason the AI could not resolve the request.</param>
/// <param name="Summary">Optional summary of the conversation for the human agent.</param>
public record EscalationRequest(string Reason, string? Summary);

/// <summary>Outcome of an escalation.</summary>
public record EscalationResult(bool WebhookDelivered, string Message);

/// <summary>
/// Marks a conversation as escalated and notifies the tenant. Implemented in the
/// Infrastructure layer: it flips the conversation status in SQL and fires the
/// tenant's configured escalation webhook (if any).
/// </summary>
public interface IEscalationService
{
    Task<EscalationResult> EscalateAsync(
        Guid tenantId, Guid conversationId, EscalationRequest request, CancellationToken ct = default);
}
