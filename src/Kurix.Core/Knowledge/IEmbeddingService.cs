namespace Kurix.Core.Knowledge;

/// <summary>
/// Produces embedding vectors for text. Backed by Azure OpenAI
/// (<c>text-embedding-3-small</c>) in production.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Embeds a single piece of text.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Embeds a batch of texts in a single request, preserving order.</summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default);
}
