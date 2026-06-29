using System.Text.Json;
using Kurix.Core.Escalation;
using Kurix.Core.Tools;

namespace Kurix.Tools;

/// <summary>
/// Marks the current conversation for human handoff. Delegates to
/// <see cref="IEscalationService"/>, which flips the conversation status in SQL
/// and fires the tenant's escalation webhook.
/// </summary>
public class EscalateToHumanTool(IEscalationService escalation) : ITool
{
    public string Name => "escalate_to_human";

    public string Description =>
        "Deriva la conversación a un agente humano. Usar cuando no podés resolver la " +
        "consulta con confianza, el cliente lo pide explícitamente, o la situación " +
        "requiere intervención humana (reclamos, casos sensibles).";

    public ToolParameterSchema GetParameterSchema() => ToolParameterSchema.Create()
        .AddString("reason", "Motivo breve por el cual se deriva a un humano.", required: true)
        .AddString("summary", "Resumen de la conversación para el agente humano.");

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context, JsonElement arguments, CancellationToken ct)
    {
        if (!arguments.TryGetRequiredString("reason", out var reason))
            return ToolResult.Fail("Falta el parámetro 'reason'.");

        var summary = arguments.GetString("summary");

        var result = await escalation.EscalateAsync(
            context.TenantId, context.ConversationId, new EscalationRequest(reason, summary), ct);

        return ToolResult.Ok(result.Message);
    }
}
