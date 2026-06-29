namespace Kurix.Core.Enums;

/// <summary>
/// Ingestion state of a knowledge-base document. Chunks and vectors live in
/// Azure AI Search; SQL only tracks metadata and ingestion lifecycle.
/// </summary>
public enum KnowledgeDocumentStatus
{
    Pending = 0,
    Processing = 1,
    Ingested = 2,
    Failed = 3
}
