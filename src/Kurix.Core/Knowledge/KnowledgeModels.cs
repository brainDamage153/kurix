namespace Kurix.Core.Knowledge;

/// <summary>Raw document submitted for ingestion into a tenant's knowledge base.</summary>
/// <param name="FileName">Source file name; used as the document identifier for later deletion.</param>
/// <param name="Content">Plain-text content of the document.</param>
/// <param name="Metadata">Optional free-form metadata stored alongside each chunk.</param>
public record KnowledgeDocumentInput(string FileName, string Content, string? Metadata = null);

/// <summary>Outcome of an ingestion operation.</summary>
/// <param name="ChunkCount">Number of chunks produced and indexed.</param>
public record KnowledgeIngestionResult(int ChunkCount);

/// <summary>A single retrieved chunk relevant to a query.</summary>
/// <param name="Content">The chunk text.</param>
/// <param name="SourceDocument">File name of the originating document.</param>
/// <param name="Score">Relevance score returned by the search backend.</param>
/// <param name="Metadata">Optional metadata carried from ingestion.</param>
public record KnowledgeSearchResult(string Content, string SourceDocument, double Score, string? Metadata);
