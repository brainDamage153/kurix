using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kurix.Core.MultiTenancy;

/// <summary>
/// Strongly-typed view of a tenant's <c>SettingsJson</c>. Drives the bot persona,
/// which tools are enabled, the escalation webhook and the fallback message.
/// </summary>
public sealed class TenantSettings
{
    /// <summary>Persona / behaviour instructions injected into the system prompt.</summary>
    public string Persona { get; set; } =
        "Sos un asistente de atención al cliente amable y profesional. " +
        "Respondé en español, de forma concisa y útil.";

    /// <summary>
    /// Names of the tools enabled for this tenant. <c>null</c> means "all
    /// registered tools are enabled"; an empty list means none.
    /// </summary>
    public List<string>? EnabledTools { get; set; }

    /// <summary>Webhook called when a conversation is escalated to a human.</summary>
    public string? EscalationWebhookUrl { get; set; }

    /// <summary>Message returned to the user when the engine cannot answer.</summary>
    public string FallbackMessage { get; set; } =
        "Disculpá, no pude procesar tu consulta en este momento. " +
        "Un agente se pondrá en contacto a la brevedad.";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Parses tenant settings JSON, returning defaults for blank or invalid input.
    /// </summary>
    public static TenantSettings Parse(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
            return new TenantSettings();

        try
        {
            return JsonSerializer.Deserialize<TenantSettings>(settingsJson, Options) ?? new TenantSettings();
        }
        catch (JsonException)
        {
            return new TenantSettings();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, Options);
}
