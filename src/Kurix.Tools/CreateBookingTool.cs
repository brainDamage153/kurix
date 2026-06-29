using System.Globalization;
using System.Text.Json;
using Kurix.Core.Connectors;
using Kurix.Core.Tools;

namespace Kurix.Tools;

/// <summary>Creates an appointment booking via the calendar connector.</summary>
public class CreateBookingTool(ICalendarConnector calendar) : ITool
{
    public string Name => "create_booking";

    public string Description =>
        "Crea una reserva o cita en la agenda. Usar cuando el cliente confirma que " +
        "quiere reservar un horario específico.";

    public ToolParameterSchema GetParameterSchema() => ToolParameterSchema.Create()
        .AddString("customerName", "Nombre del cliente que reserva.", required: true)
        .AddString("dateTime", "Fecha y hora de la reserva en formato ISO 8601 (ej: 2026-07-01T15:00).", required: true)
        .AddString("serviceType", "Tipo de servicio a reservar, si aplica.")
        .AddString("notes", "Notas adicionales para la reserva.");

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context, JsonElement arguments, CancellationToken ct)
    {
        if (!arguments.TryGetRequiredString("customerName", out var customerName))
            return ToolResult.Fail("Falta el parámetro 'customerName'.");

        if (!arguments.TryGetRequiredString("dateTime", out var dateTimeRaw))
            return ToolResult.Fail("Falta el parámetro 'dateTime'.");

        if (!DateTimeOffset.TryParse(
                dateTimeRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var start))
            return ToolResult.Fail($"Fecha/hora inválida: '{dateTimeRaw}'. Usá formato ISO 8601.");

        var request = new BookingRequest(
            customerName,
            start,
            arguments.GetString("serviceType"),
            arguments.GetString("notes"));

        var result = await calendar.CreateBookingAsync(context.TenantId, request, ct);

        return result.Success
            ? ToolResult.Ok(result.Message)
            : ToolResult.Fail(result.Message);
    }
}
