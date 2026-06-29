namespace Kurix.Infrastructure.Configuration;

/// <summary>
/// Binds the <c>Rag</c> configuration section: chunking parameters and retrieval
/// defaults for the knowledge base.
/// </summary>
public class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>Maximum tokens per chunk.</summary>
    public int ChunkSizeTokens { get; set; } = 500;

    /// <summary>Tokens of overlap between consecutive chunks.</summary>
    public int ChunkOverlapTokens { get; set; } = 50;

    /// <summary>Default number of chunks to retrieve when not specified.</summary>
    public int DefaultTopK { get; set; } = 4;
}
