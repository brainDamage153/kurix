using System.ClientModel;
using Azure.AI.OpenAI;
using Kurix.Core.Knowledge;
using Kurix.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace Kurix.Infrastructure.Knowledge;

/// <summary>
/// <see cref="IEmbeddingService"/> backed by an Azure OpenAI embeddings
/// deployment (e.g. <c>text-embedding-3-small</c>).
/// </summary>
public class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly EmbeddingGenerationOptions _options;

    public AzureOpenAIEmbeddingService(IOptions<AzureOpenAIOptions> options)
    {
        var o = options.Value;
        var azureClient = new AzureOpenAIClient(new Uri(o.Endpoint), new ApiKeyCredential(o.ApiKey));
        _client = azureClient.GetEmbeddingClient(o.EmbeddingDeployment);
        _options = new EmbeddingGenerationOptions { Dimensions = o.EmbeddingDimensions };
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        ClientResult<OpenAIEmbedding> result = await _client.GenerateEmbeddingAsync(text, _options, ct);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (texts.Count == 0)
            return [];

        ClientResult<OpenAIEmbeddingCollection> result =
            await _client.GenerateEmbeddingsAsync(texts, _options, ct);
        return result.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
