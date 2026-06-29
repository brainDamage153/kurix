namespace Kurix.Core.Conversations;

/// <summary>Outcome of processing one user message.</summary>
/// <param name="Reply">The assistant's reply to show the user.</param>
/// <param name="Escalated">True if the conversation was handed off to a human.</param>
/// <param name="ToolsUsed">Names of tools invoked during the turn.</param>
/// <param name="TokensIn">Total prompt tokens for the turn.</param>
/// <param name="TokensOut">Total completion tokens for the turn.</param>
public sealed record ConversationTurnResult(
    string Reply,
    bool Escalated,
    IReadOnlyList<string> ToolsUsed,
    int TokensIn,
    int TokensOut);

/// <summary>
/// The conversational engine. For one user message it: loads history, retrieves
/// RAG context, builds the system prompt, exposes the tenant's enabled tools to
/// the model, runs a bounded tool-calling loop, persists the turn and metrics,
/// and returns the reply.
/// </summary>
public interface IConversationService
{
    Task<ConversationTurnResult> ProcessMessageAsync(
        Guid tenantId, Guid conversationId, string userMessage, CancellationToken ct = default);
}
