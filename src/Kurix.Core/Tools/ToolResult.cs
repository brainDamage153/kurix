namespace Kurix.Core.Tools;

/// <summary>
/// Outcome of a tool execution. <see cref="Content"/> is fed back to the model as
/// the tool's result message; on failure, <see cref="Error"/> explains why so the
/// model can recover or escalate.
/// </summary>
public sealed record ToolResult(bool Success, string Content, string? Error = null)
{
    public static ToolResult Ok(string content) => new(true, content);

    public static ToolResult Fail(string error) => new(false, string.Empty, error);
}
