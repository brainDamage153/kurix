using System.Text.Json;
using Kurix.Core.Conversations;
using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.Knowledge;
using Kurix.Core.MultiTenancy;
using Kurix.Core.Tools;
using Kurix.Infrastructure.Configuration;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurix.Infrastructure.Conversations;

/// <summary>
/// Orchestrates a single conversational turn end to end: history + RAG +
/// system prompt + bounded tool-calling loop against the chat model, persisting
/// every message and the token usage, and degrading to a fallback reply on error.
/// </summary>
public class ConversationService(
    KurixDbContext db,
    IKnowledgeService knowledge,
    IToolRegistry toolRegistry,
    IChatCompletionService chat,
    IOptions<ConversationOptions> conversationOptions,
    IOptions<RagOptions> ragOptions,
    ILogger<ConversationService> logger) : IConversationService
{
    private readonly ConversationOptions _options = conversationOptions.Value;
    private readonly RagOptions _rag = ragOptions.Value;

    public async Task<ConversationTurnResult> ProcessMessageAsync(
        Guid tenantId, Guid conversationId, string userMessage, CancellationToken ct = default)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException(
                $"Conversation {conversationId} not found for tenant {tenantId}.");

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");

        var settings = TenantSettings.Parse(tenant.SettingsJson);

        // Persist the incoming user message before doing any work.
        db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = userMessage
        });
        await db.SaveChangesAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var toolsUsed = new List<string>();
        var tokensIn = 0;
        var tokensOut = 0;
        string reply;

        try
        {
            (reply, tokensIn, tokensOut) = await RunEngineAsync(
                tenant, conversation, userMessage, settings, toolsUsed, timeoutCts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Azure errors, timeouts, malformed responses: degrade gracefully.
            logger.LogError(ex, "Conversation engine failed for conversation {ConversationId}.", conversationId);
            reply = settings.FallbackMessage;
        }

        // The escalate tool flips status within this same DbContext, so re-reading
        // the tracked entity reflects whether we escalated.
        var escalated = conversation.Status == ConversationStatus.Escalated;

        db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = reply,
            TokensIn = tokensIn,
            TokensOut = tokensOut
        });
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ConversationTurnResult(reply, escalated, toolsUsed, tokensIn, tokensOut);
    }

    private async Task<(string Reply, int TokensIn, int TokensOut)> RunEngineAsync(
        Tenant tenant,
        Conversation conversation,
        string userMessage,
        TenantSettings settings,
        List<string> toolsUsed,
        CancellationToken ct)
    {
        // 1. RAG context.
        var context = await knowledge.SearchAsync(tenant.Id, userMessage, _rag.DefaultTopK, ct);

        // 2. Build the message list: system prompt + history + current message.
        var messages = new List<LlmMessage>
        {
            LlmMessage.System(SystemPromptBuilder.Build(settings, context))
        };
        messages.AddRange(await LoadHistoryAsync(conversation.Id, ct));
        messages.Add(LlmMessage.User(userMessage));

        // 3. Enabled tools as function definitions.
        var enabledTools = toolRegistry.GetEnabledTools(tenant);
        var toolDefs = enabledTools
            .Select(t => new ToolDefinition(t.Name, t.Description, t.GetParameterSchema().ToJsonSchema()))
            .ToList();

        var execContext = new ToolExecutionContext(tenant.Id, conversation.Id, conversation.SessionId);

        var tokensIn = 0;
        var tokensOut = 0;

        // 4. Bounded tool-calling loop.
        for (var iteration = 0; iteration < _options.MaxToolIterations; iteration++)
        {
            var completion = await chat.CompleteAsync(messages, toolDefs, ct);
            tokensIn += completion.InputTokens;
            tokensOut += completion.OutputTokens;

            if (!completion.HasToolCalls)
                return (completion.Text ?? settings.FallbackMessage, tokensIn, tokensOut);

            // Echo the assistant's tool-call turn, then run each tool and feed results back.
            messages.Add(LlmMessage.AssistantToolCalls(completion.ToolCalls));

            foreach (var call in completion.ToolCalls)
            {
                var result = await ExecuteToolAsync(call, execContext, ct);
                toolsUsed.Add(call.ToolName);

                db.Messages.Add(new Message
                {
                    ConversationId = conversation.Id,
                    Role = MessageRole.Tool,
                    ToolName = call.ToolName,
                    Content = result.Success ? result.Content : $"ERROR: {result.Error}"
                });

                messages.Add(LlmMessage.ToolResult(
                    call.Id, result.Success ? result.Content : $"ERROR: {result.Error}"));
            }
        }

        // Loop exhausted without a final answer.
        logger.LogWarning(
            "Tool-calling loop hit the {Max}-iteration limit for conversation {ConversationId}.",
            _options.MaxToolIterations, conversation.Id);
        return (settings.FallbackMessage, tokensIn, tokensOut);
    }

    private async Task<ToolResult> ExecuteToolAsync(
        LlmToolCall call, ToolExecutionContext context, CancellationToken ct)
    {
        var tool = toolRegistry.GetTool(call.ToolName);
        if (tool is null)
            return ToolResult.Fail($"La herramienta '{call.ToolName}' no está disponible.");

        try
        {
            using var doc = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            return await tool.ExecuteAsync(context, doc.RootElement.Clone(), ct);
        }
        catch (JsonException)
        {
            return ToolResult.Fail("Los argumentos de la herramienta no son JSON válido.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool {Tool} threw during execution.", call.ToolName);
            return ToolResult.Fail($"La herramienta '{call.ToolName}' falló al ejecutarse.");
        }
    }

    private async Task<IReadOnlyList<LlmMessage>> LoadHistoryAsync(Guid conversationId, CancellationToken ct)
    {
        // Replay only user/assistant content turns (newest N, then chronological).
        // Tool rows are kept for audit but not replayed, to avoid dangling
        // tool-call references in the prompt.
        var recent = await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId &&
                        (m.Role == MessageRole.User || m.Role == MessageRole.Assistant))
            .OrderByDescending(m => m.CreatedAt)
            .Take(_options.HistoryLimit)
            .ToListAsync(ct);

        recent.Reverse();

        // Drop the just-persisted user message (it's appended separately as the current turn).
        if (recent.Count > 0 && recent[^1].Role == MessageRole.User)
            recent.RemoveAt(recent.Count - 1);

        return recent
            .Select(m => m.Role == MessageRole.User
                ? LlmMessage.User(m.Content)
                : LlmMessage.Assistant(m.Content))
            .ToList();
    }
}
