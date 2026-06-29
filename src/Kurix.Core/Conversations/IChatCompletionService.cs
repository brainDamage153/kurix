namespace Kurix.Core.Conversations;

/// <summary>A tool exposed to the model as a function definition.</summary>
/// <param name="Name">Function name.</param>
/// <param name="Description">When-to-use description.</param>
/// <param name="ParametersJsonSchema">JSON Schema of the parameters.</param>
public sealed record ToolDefinition(string Name, string Description, string ParametersJsonSchema);

/// <summary>Result of a single model completion call.</summary>
/// <param name="Text">Assistant text, when the model produced a final answer.</param>
/// <param name="ToolCalls">Tool calls the model wants executed, if any.</param>
/// <param name="InputTokens">Prompt tokens consumed.</param>
/// <param name="OutputTokens">Completion tokens produced.</param>
public sealed record ChatCompletionResult(
    string? Text, IReadOnlyList<LlmToolCall> ToolCalls, int InputTokens, int OutputTokens)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}

/// <summary>
/// Thin abstraction over the chat model (Azure OpenAI <c>gpt-4o-mini</c>). One
/// call = one round-trip; the tool-calling loop lives in the conversation
/// service, keeping this interface simple and the loop testable.
/// </summary>
public interface IChatCompletionService
{
    Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken ct = default);
}
