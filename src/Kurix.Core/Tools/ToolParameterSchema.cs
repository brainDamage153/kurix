using System.Text.Json.Nodes;

namespace Kurix.Core.Tools;

/// <summary>A single parameter of a tool, used to build its JSON Schema.</summary>
/// <param name="Name">Parameter name.</param>
/// <param name="Type">JSON Schema type: <c>string</c>, <c>number</c>, <c>integer</c>, <c>boolean</c>.</param>
/// <param name="Description">Natural-language description shown to the model.</param>
/// <param name="Required">Whether the parameter is required.</param>
/// <param name="Enum">Optional set of allowed values.</param>
public record ToolParameter(
    string Name, string Type, string Description, bool Required, IReadOnlyList<string>? Enum = null);

/// <summary>
/// Describes the parameters a tool accepts and renders them as a JSON Schema
/// object — the form Azure OpenAI function definitions expect. Built fluently by
/// each tool in <c>GetParameterSchema</c>.
/// </summary>
public sealed class ToolParameterSchema
{
    private readonly List<ToolParameter> _parameters = [];

    public IReadOnlyList<ToolParameter> Parameters => _parameters;

    public static ToolParameterSchema Create() => new();

    /// <summary>A schema with no parameters (still a valid empty object schema).</summary>
    public static ToolParameterSchema Empty => new();

    public ToolParameterSchema AddString(
        string name, string description, bool required = false, IReadOnlyList<string>? @enum = null)
        => Add(name, "string", description, required, @enum);

    public ToolParameterSchema AddNumber(string name, string description, bool required = false)
        => Add(name, "number", description, required);

    public ToolParameterSchema AddInteger(string name, string description, bool required = false)
        => Add(name, "integer", description, required);

    public ToolParameterSchema AddBoolean(string name, string description, bool required = false)
        => Add(name, "boolean", description, required);

    private ToolParameterSchema Add(
        string name, string type, string description, bool required, IReadOnlyList<string>? @enum = null)
    {
        _parameters.Add(new ToolParameter(name, type, description, required, @enum));
        return this;
    }

    /// <summary>Renders this schema as a JSON Schema object string.</summary>
    public string ToJsonSchema()
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var p in _parameters)
        {
            var prop = new JsonObject
            {
                ["type"] = p.Type,
                ["description"] = p.Description
            };
            if (p.Enum is { Count: > 0 })
            {
                var values = new JsonArray();
                foreach (var v in p.Enum)
                    values.Add(v);
                prop["enum"] = values;
            }
            properties[p.Name] = prop;

            if (p.Required)
                required.Add(p.Name);
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required
        };

        return schema.ToJsonString();
    }
}
