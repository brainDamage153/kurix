using System.Text.Json;
using Kurix.Core.Tools;

namespace Kurix.Tests.Tools;

public class ToolParameterSchemaTests
{
    [Fact]
    public void Empty_Schema_Is_Valid_Object_With_No_Properties()
    {
        var json = ToolParameterSchema.Empty.ToJsonSchema();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
        Assert.Empty(doc.RootElement.GetProperty("properties").EnumerateObject());
        Assert.Equal(0, doc.RootElement.GetProperty("required").GetArrayLength());
    }

    [Fact]
    public void Schema_Renders_Properties_Types_Required_And_Enum()
    {
        var json = ToolParameterSchema.Create()
            .AddString("date", "La fecha", required: true)
            .AddString("status", "Estado", @enum: ["open", "closed"])
            .AddInteger("count", "Cantidad")
            .ToJsonSchema();

        using var doc = JsonDocument.Parse(json);
        var props = doc.RootElement.GetProperty("properties");

        Assert.Equal("string", props.GetProperty("date").GetProperty("type").GetString());
        Assert.Equal("La fecha", props.GetProperty("date").GetProperty("description").GetString());
        Assert.Equal("integer", props.GetProperty("count").GetProperty("type").GetString());

        var enumValues = props.GetProperty("status").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(["open", "closed"], enumValues);

        var required = doc.RootElement.GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(["date"], required);
    }
}
