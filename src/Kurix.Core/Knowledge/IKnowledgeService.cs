namespace Kurix.Core.Knowledge;

/// <summary>
/// Retrieval-Augmented Generation surface for a tenant's knowledge base.
/// Implementations chunk + embed + index documents and run hybrid (vector + text)
/// retrieval. Every operation is scoped to a single <c>tenantId</c>: the
/// underlying Azure AI Search index is shared across tenants and <b>must</b> be
/// filtered by tenant on every query.
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// Chunks the document by tokens (with overlap), embeds each chunk and
    /// indexes it under the given tenant. Returns how many chunks were indexed.
    /// </summary>
    Task<KnowledgeIngestionResult> IngestAsync(
        Guid tenantId, KnowledgeDocumentInput document, CancellationToken ct = default);

    /// <summary>
    /// Embeds the query and runs a hybrid search filtered by tenant, returning
    /// the most relevant chunks.
    /// </summary>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId, string query, int topK = 4, CancellationToken ct = default);

    /// <summary>
    /// Removes every chunk that originated from the given source document for the
    /// tenant. Used when a document is deleted from the dashboard.
    /// </summary>
    Task DeleteDocumentAsync(
        Guid tenantId, string sourceDocument, CancellationToken ct = default);
}
