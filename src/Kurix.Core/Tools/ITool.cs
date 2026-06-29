using System.Text.Json;

namespace Kurix.Core.Tools;

/// <summary>
/// A capability the AI can invoke via function calling. This is the central
/// extension point of the engine: new actions (current and future modules) are
/// added by implementing this interface and registering it — no changes to the
/// conversation loop are required.
/// </summary>
public interface ITool
{
    /// <summary>Unique function name exposed to the model (snake_case by convention).</summary>
    string Name { get; }

    /// <summary>Natural-language description that tells the model when to use the tool.</summary>
    string Description { get; }

    /// <summary>Schema of the arguments the tool accepts.</summary>
    ToolParameterSchema GetParameterSchema();

    /// <summary>
    /// Executes the tool. <paramref name="arguments"/> is the JSON object the model
    /// produced, validated loosely; implementations read the fields they declared.
    /// </summary>
    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context, JsonElement arguments, CancellationToken ct);
}
