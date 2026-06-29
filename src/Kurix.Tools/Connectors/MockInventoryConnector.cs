using Kurix.Core.Connectors;

namespace Kurix.Tools.Connectors;

/// <summary>
/// Demo <see cref="IInventoryConnector"/> backed by a small in-memory catalog.
/// Replaced per client by a real integration that implements the same interface.
/// </summary>
public class MockInventoryConnector : IInventoryConnector
{
    private static readonly IReadOnlyList<InventoryItem> Catalog =
    [
        new("SKU-1001", "Notebook 14\" Core i5", 799_000m, 12),
        new("SKU-1002", "Notebook 15\" Core i7", 1_199_000m, 5),
        new("SKU-2001", "Mouse inalámbrico", 19_990m, 80),
        new("SKU-2002", "Teclado mecánico", 49_990m, 30),
        new("SKU-3001", "Monitor 24\" Full HD", 129_990m, 0),
        new("SKU-3002", "Monitor 27\" 2K", 229_990m, 7)
    ];

    public Task<IReadOnlyList<InventoryItem>> SearchAsync(
        Guid tenantId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(Catalog);

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = Catalog
            .Where(item => terms.Any(t =>
                item.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                item.Sku.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryItem>>(matches);
    }
}
