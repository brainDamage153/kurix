using Kurix.Api.Contracts;
using Kurix.Core.Conversations;
using Kurix.Core.MultiTenancy;
using Kurix.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kurix.Api.Controllers;

/// <summary>
/// Widget endpoint. Authentication is by API key, resolved into
/// <see cref="ITenantContext"/> by the tenant-resolution middleware.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatController(
    ITenantContext tenantContext,
    IConversationRepository conversations,
    IConversationService engine) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (!tenantContext.IsResolved)
            return Unauthorized(new { error = "API key requerida o inválida." });

        var tenantId = tenantContext.TenantId;
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId;

        var conversation = await conversations.GetOrCreateBySessionAsync(tenantId, sessionId, ct);
        var result = await engine.ProcessMessageAsync(tenantId, conversation.Id, request.Message, ct);

        return Ok(new ChatResponse(
            result.Reply, result.Escalated, sessionId, conversation.Id, result.ToolsUsed));
    }
}
