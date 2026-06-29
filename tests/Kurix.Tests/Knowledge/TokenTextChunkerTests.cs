using Kurix.Infrastructure.Knowledge;
using Microsoft.ML.Tokenizers;

namespace Kurix.Tests.Knowledge;

public class TokenTextChunkerTests
{
    private readonly TokenTextChunker _chunker = new();
    private readonly TiktokenTokenizer _tokenizer = TiktokenTokenizer.CreateForEncoding("cl100k_base");

    [Fact]
    public void Blank_Input_Returns_Empty()
    {
        Assert.Empty(_chunker.Chunk("", 100, 10));
        Assert.Empty(_chunker.Chunk("   ", 100, 10));
    }

    [Fact]
    public void Short_Text_Returns_Single_Chunk()
    {
        var chunks = _chunker.Chunk("Hola, ¿cómo estás?", maxTokens: 100, overlapTokens: 10);
        Assert.Single(chunks);
    }

    [Fact]
    public void Long_Text_Is_Split_Into_Multiple_Chunks_Within_Token_Limit()
    {
        // ~600 words -> well over 100 tokens.
        var text = string.Join(" ", Enumerable.Range(0, 600).Select(i => $"palabra{i}"));

        var chunks = _chunker.Chunk(text, maxTokens: 100, overlapTokens: 20);

        Assert.True(chunks.Count > 1);
        foreach (var chunk in chunks)
            Assert.True(_tokenizer.CountTokens(chunk) <= 100, "Chunk exceeded max token budget.");
    }

    [Fact]
    public void Consecutive_Chunks_Overlap()
    {
        var text = string.Join(" ", Enumerable.Range(0, 400).Select(i => $"token{i}"));

        var chunks = _chunker.Chunk(text, maxTokens: 80, overlapTokens: 20);

        Assert.True(chunks.Count >= 2);
        // Each word is unique, so any shared word between consecutive chunks can
        // only come from the overlap window.
        var first = chunks[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var second = chunks[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        Assert.True(first.Overlaps(second), "Consecutive chunks did not share overlap content.");
    }

    [Theory]
    [InlineData(0, 0)]      // maxTokens must be positive
    [InlineData(50, 50)]    // overlap must be < maxTokens
    [InlineData(50, -1)]    // overlap must be >= 0
    public void Invalid_Parameters_Throw(int maxTokens, int overlap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _chunker.Chunk("some text", maxTokens, overlap));
    }
}
