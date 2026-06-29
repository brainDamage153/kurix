using Kurix.Api.Auth;
using Kurix.Api.Contracts;
using Kurix.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kurix.Api.Controllers;

/// <summary>Conversation browsing for the dashboard (JWT). Scoped to the caller's tenant.</summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController(IConversationRepository conversations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryResponse>>> List(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var list = await conversations.ListAsync(tenantId, ct: ct);

        return Ok(list.Select(c => new ConversationSummaryResponse(
            c.Id, c.SessionId, c.Status.ToString(), c.StartedAt, c.LastMessageAt)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var conversation = await conversations.GetWithMessagesAsync(tenantId, id, ct);

        if (conversation is null)
            return NotFound();

        var messages = conversation.Messages
            .Select(m => new MessageResponse(
                m.Role.ToString(), m.Content, m.ToolName, m.CreatedAt))
            .ToList();

        return Ok(new ConversationDetailResponse(
            conversation.Id, conversation.SessionId, conversation.Status.ToString(),
            conversation.StartedAt, conversation.LastMessageAt, messages));
    }
}
