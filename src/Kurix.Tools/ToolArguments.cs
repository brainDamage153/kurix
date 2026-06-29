using System.Text.Json;

namespace Kurix.Tools;

/// <summary>
/// Helpers for reading values out of the JSON argument object the model produces,
/// tolerating missing fields and loose typing.
/// </summary>
internal static class ToolArguments
{
    public static string? GetString(this JsonElement args, string name)
    {
        if (args.ValueKind != JsonValueKind.Object ||
            !args.TryGetProperty(name, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => prop.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => prop.GetRawText(),
            _ => null
        };
    }

    public static bool TryGetRequiredString(
        this JsonElement args, string name, out string value)
    {
        var raw = args.GetString(name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = string.Empty;
            return false;
        }
        value = raw;
        return true;
    }
}
