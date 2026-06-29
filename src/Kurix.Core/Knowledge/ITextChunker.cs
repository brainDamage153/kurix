namespace Kurix.Core.Knowledge;

/// <summary>
/// Splits text into overlapping chunks sized by token count, so each chunk fits
/// comfortably within the embedding model's context and neighbouring chunks share
/// context at their boundaries.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Chunks <paramref name="text"/> into segments of at most
    /// <paramref name="maxTokens"/> tokens, each overlapping the previous by
    /// <paramref name="overlapTokens"/> tokens. Returns an empty list for blank input.
    /// </summary>
    IReadOnlyList<string> Chunk(string text, int maxTokens, int overlapTokens);
}
