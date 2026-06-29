namespace Kurix.Core.Connectors;

/// <summary>An item in a tenant's inventory.</summary>
public record InventoryItem(string Sku, string Name, decimal Price, int Stock);

/// <summary>
/// Abstraction over a tenant's inventory/catalog system. Real integrations are
/// added later by implementing this interface per client; the MVP ships a mock.
/// </summary>
public interface IInventoryConnector
{
    Task<IReadOnlyList<InventoryItem>> SearchAsync(
        Guid tenantId, string query, CancellationToken ct = default);
}
