namespace Kurix.Infrastructure.Configuration;

/// <summary>
/// Binds the <c>AzureAISearch</c> configuration section. A single shared index
/// holds knowledge chunks for all tenants; every query is filtered by
/// <c>tenantId</c>.
/// </summary>
public class AzureAISearchOptions
{
    public const string SectionName = "AzureAISearch";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Name of the shared knowledge index.</summary>
    public string IndexName { get; set; } = "kurix-knowledge";
}
