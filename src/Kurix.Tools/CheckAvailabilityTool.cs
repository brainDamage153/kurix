using System.Globalization;
using System.Text;
using System.Text.Json;
using Kurix.Core.Connectors;
using Kurix.Core.Tools;

namespace Kurix.Tools;

/// <summary>Looks up available appointment slots for a date via the calendar connector.</summary>
public class CheckAvailabilityTool(ICalendarConnector calendar) : ITool
{
    public string Name => "check_availability";

    public string Description =>
        "Consulta los horarios disponibles en la agenda para una fecha específica. " +
        "Usar cuando el cliente quiere saber qué horarios hay libres.";

    public ToolParameterSchema GetParameterSchema() => ToolParameterSchema.Create()
        .AddString("date", "Fecha a consultar en formato YYYY-MM-DD.", required: true)
        .AddString("serviceType", "Tipo de servicio, si aplica.");

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context, JsonElement arguments, CancellationToken ct)
    {
        if (!arguments.TryGetRequiredString("date", out var dateRaw))
            return ToolResult.Fail("Falta el parámetro 'date' (YYYY-MM-DD).");

        if (!DateOnly.TryParse(dateRaw, CultureInfo.InvariantCulture, out var date))
            return ToolResult.Fail($"Fecha inválida: '{dateRaw}'. Usá el formato YYYY-MM-DD.");

        var serviceType = arguments.GetString("serviceType");
        var slots = await calendar.GetAvailabilityAsync(context.TenantId, date, serviceType, ct);

        if (slots.Count == 0)
            return ToolResult.Ok($"No hay horarios disponibles para el {date:dd/MM/yyyy}.");

        var sb = new StringBuilder();
        sb.AppendLine($"Horarios disponibles para el {date:dd/MM/yyyy}:");
        foreach (var slot in slots)
            sb.AppendLine($"- {slot.Start:HH:mm} a {slot.End:HH:mm}");

        return ToolResult.Ok(sb.ToString().TrimEnd());
    }
}
