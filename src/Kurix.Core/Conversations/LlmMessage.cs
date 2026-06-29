using Kurix.Core.Enums;

namespace Kurix.Core.Conversations;

/// <summary>A tool/function call requested by the model.</summary>
/// <param name="Id">Provider-assigned id, used to correlate the tool result.</param>
/// <param name="ToolName">Name of the tool to invoke.</param>
/// <param name="ArgumentsJson">Raw JSON arguments produced by the model.</param>
public sealed record LlmToolCall(string Id, string ToolName, string ArgumentsJson);

/// <summary>
/// Provider-agnostic chat message exchanged with the language model. Keeps the
/// conversation loop independent of any specific SDK so it can be unit-tested
/// with a fake completion service.
/// </summary>
public sealed record LlmMessage
{
    public required MessageRole Role { get; init; }
    public string? Content { get; init; }

    /// <summary>Tool calls requested by an assistant turn.</summary>
    public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = [];

    /// <summary>Set on a <see cref="MessageRole.Tool"/> message to the call it answers.</summary>
    public string? ToolCallId { get; init; }

    public static LlmMessage System(string content) =>
        new() { Role = MessageRole.System, Content = content };

    public static LlmMessage User(string content) =>
        new() { Role = MessageRole.User, Content = content };

    public static LlmMessage Assistant(string content) =>
        new() { Role = MessageRole.Assistant, Content = content };

    public static LlmMessage AssistantToolCalls(IReadOnlyList<LlmToolCall> toolCalls) =>
        new() { Role = MessageRole.Assistant, ToolCalls = toolCalls };

    public static LlmMessage ToolResult(string toolCallId, string content) =>
        new() { Role = MessageRole.Tool, ToolCallId = toolCallId, Content = content };
}
