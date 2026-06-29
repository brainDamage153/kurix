using Kurix.Core.Knowledge;
using Microsoft.ML.Tokenizers;

namespace Kurix.Infrastructure.Knowledge;

/// <summary>
/// Token-based chunker using the <c>cl100k_base</c> tiktoken encoding — the
/// encoding used by <c>text-embedding-3-small</c> — so chunk sizes line up with
/// what the embedding model actually consumes. Chunks are produced with a
/// configurable token overlap to preserve context across boundaries.
/// </summary>
public class TokenTextChunker : ITextChunker
{
    private readonly TiktokenTokenizer _tokenizer = TiktokenTokenizer.CreateForEncoding("cl100k_base");

    public IReadOnlyList<string> Chunk(string text, int maxTokens, int overlapTokens)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "maxTokens must be positive.");
        if (overlapTokens < 0 || overlapTokens >= maxTokens)
            throw new ArgumentOutOfRangeException(nameof(overlapTokens), "overlapTokens must be in [0, maxTokens).");

        var ids = _tokenizer.EncodeToIds(text);
        if (ids.Count <= maxTokens)
            return [text.Trim()];

        var step = maxTokens - overlapTokens;
        var chunks = new List<string>();

        for (var start = 0; start < ids.Count; start += step)
        {
            var window = ids.Skip(start).Take(maxTokens).ToArray();
            var chunk = _tokenizer.Decode(window).Trim();
            if (chunk.Length > 0)
                chunks.Add(chunk);

            if (start + maxTokens >= ids.Count)
                break;
        }

        return chunks;
    }
}
