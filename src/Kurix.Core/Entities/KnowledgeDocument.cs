using Kurix.Core.Enums;

namespace Kurix.Core.Entities;

/// <summary>
/// Metadata for a document ingested into a tenant's knowledge base. The actual
/// chunks and embedding vectors live in Azure AI Search; this row only tracks
/// the source document and its ingestion lifecycle.
/// </summary>
public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public required string FileName { get; set; }

    public KnowledgeDocumentStatus Status { get; set; } = KnowledgeDocumentStatus.Pending;

    public int ChunkCount { get; set; }

    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Populated when <see cref="Status"/> is <c>Failed</c>.</summary>
    public string? ErrorMessage { get; set; }

    // Navigation
    public Tenant? Tenant { get; set; }
}
