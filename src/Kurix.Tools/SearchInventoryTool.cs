using System.Globalization;
using System.Text;
using System.Text.Json;
using Kurix.Core.Connectors;
using Kurix.Core.Tools;

namespace Kurix.Tools;

/// <summary>Searches the tenant's inventory/catalog via the inventory connector.</summary>
public class SearchInventoryTool(IInventoryConnector inventory) : ITool
{
    public string Name => "search_inventory";

    public string Description =>
        "Busca productos en el inventario por nombre o palabra clave, devolviendo " +
        "precio y stock. Usar cuando el cliente pregunta por productos o disponibilidad de stock.";

    public ToolParameterSchema GetParameterSchema() => ToolParameterSchema.Create()
        .AddString("query", "Texto o palabra clave a buscar en el catálogo.", required: true);

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context, JsonElement arguments, CancellationToken ct)
    {
        if (!arguments.TryGetRequiredString("query", out var query))
            return ToolResult.Fail("Falta el parámetro 'query'.");

        var items = await inventory.SearchAsync(context.TenantId, query, ct);

        if (items.Count == 0)
            return ToolResult.Ok($"No se encontraron productos para '{query}'.");

        var sb = new StringBuilder();
        sb.AppendLine($"Resultados para '{query}':");
        foreach (var item in items)
        {
            var stock = item.Stock > 0 ? $"{item.Stock} en stock" : "sin stock";
            sb.AppendLine(
                $"- {item.Name} ({item.Sku}): " +
                $"{item.Price.ToString("C0", CultureInfo.GetCultureInfo("es-CL"))} — {stock}");
        }

        return ToolResult.Ok(sb.ToString().TrimEnd());
    }
}
