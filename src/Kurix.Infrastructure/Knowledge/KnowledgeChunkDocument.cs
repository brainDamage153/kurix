using System.Text.Json.Serialization;

namespace Kurix.Infrastructure.Knowledge;

/// <summary>
/// Shape of a single document in the shared <c>kurix-knowledge</c> Azure AI
/// Search index. One row per chunk. JSON property names match the index field
/// names exactly.
/// </summary>
public class KnowledgeChunkDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Owning tenant — every query filters on this field.</summary>
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("contentVector")]
    public float[] ContentVector { get; set; } = [];

    [JsonPropertyName("sourceDocument")]
    public string SourceDocument { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}
