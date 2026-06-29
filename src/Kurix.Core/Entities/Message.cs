using Kurix.Core.Enums;

namespace Kurix.Core.Entities;

/// <summary>
/// A single turn within a conversation: a user message, an assistant reply,
/// or a tool invocation/result. Token counts power usage and cost metrics.
/// </summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    public MessageRole Role { get; set; }

    public required string Content { get; set; }

    /// <summary>Name of the tool, when <see cref="Role"/> is <c>Tool</c>.</summary>
    public string? ToolName { get; set; }

    public int TokensIn { get; set; }

    public int TokensOut { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Conversation? Conversation { get; set; }
}
