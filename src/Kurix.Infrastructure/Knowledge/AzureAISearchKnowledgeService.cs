using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Kurix.Core.Knowledge;
using Kurix.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurix.Infrastructure.Knowledge;

/// <summary>
/// <see cref="IKnowledgeService"/> over a single shared Azure AI Search index.
/// Tenant isolation is enforced by stamping every chunk with its
/// <c>tenantId</c> and filtering on it in every query and delete. Retrieval is
/// hybrid: the query text drives keyword scoring while its embedding drives
/// vector similarity, fused by the service.
/// </summary>
public class AzureAISearchKnowledgeService : IKnowledgeService
{
    private const string VectorField = "contentVector";
    private const string VectorProfile = "kurix-vector-profile";
    private const string HnswConfig = "kurix-hnsw";
    private const int MaxDeleteFetch = 1000;

    private readonly SearchIndexClient _indexClient;
    private readonly IEmbeddingService _embeddings;
    private readonly ITextChunker _chunker;
    private readonly AzureAISearchOptions _searchOptions;
    private readonly RagOptions _ragOptions;
    private readonly int _embeddingDimensions;
    private readonly ILogger<AzureAISearchKnowledgeService> _logger;

    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexEnsured;

    public AzureAISearchKnowledgeService(
        SearchIndexClient indexClient,
        IEmbeddingService embeddings,
        ITextChunker chunker,
        IOptions<AzureAISearchOptions> searchOptions,
        IOptions<RagOptions> ragOptions,
        IOptions<AzureOpenAIOptions> openAIOptions,
        ILogger<AzureAISearchKnowledgeService> logger)
    {
        _indexClient = indexClient;
        _embeddings = embeddings;
        _chunker = chunker;
        _searchOptions = searchOptions.Value;
        _ragOptions = ragOptions.Value;
        _embeddingDimensions = openAIOptions.Value.EmbeddingDimensions;
        _logger = logger;
    }

    public async Task<KnowledgeIngestionResult> IngestAsync(
        Guid tenantId, KnowledgeDocumentInput document, CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);

        var chunks = _chunker.Chunk(
            document.Content, _ragOptions.ChunkSizeTokens, _ragOptions.ChunkOverlapTokens);
        if (chunks.Count == 0)
            return new KnowledgeIngestionResult(0);

        var vectors = await _embeddings.EmbedBatchAsync(chunks, ct);

        var docs = chunks.Select((content, i) => new KnowledgeChunkDocument
        {
            Id = $"{tenantId:N}-{Guid.NewGuid():N}",
            TenantId = tenantId.ToString(),
            Content = content,
            ContentVector = vectors[i],
            SourceDocument = document.FileName,
            Metadata = document.Metadata
        }).ToList();

        var client = _indexClient.GetSearchClient(_searchOptions.IndexName);
        await client.MergeOrUploadDocumentsAsync(docs, cancellationToken: ct);

        _logger.LogInformation(
            "Indexed {Count} chunks for tenant {TenantId} from {Source}.",
            docs.Count, tenantId, document.FileName);

        return new KnowledgeIngestionResult(docs.Count);
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        Guid tenantId, string query, int topK = 4, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];
        if (topK <= 0)
            topK = _ragOptions.DefaultTopK;

        var queryVector = await _embeddings.EmbedAsync(query, ct);

        var options = new SearchOptions
        {
            // Tenant isolation: never search outside the caller's tenant.
            Filter = TenantFilter(tenantId),
            Size = topK,
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { VectorField }
                    }
                }
            }
        };
        options.Select.Add("content");
        options.Select.Add("sourceDocument");
        options.Select.Add("metadata");

        var client = _indexClient.GetSearchClient(_searchOptions.IndexName);

        // Passing both query text and a vector query makes this a hybrid search.
        var response = await client.SearchAsync<KnowledgeChunkDocument>(query, options, ct);

        var results = new List<KnowledgeSearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var doc = result.Document;
            results.Add(new KnowledgeSearchResult(
                doc.Content, doc.SourceDocument, result.Score ?? 0d, doc.Metadata));
        }

        return results;
    }

    public async Task DeleteDocumentAsync(
        Guid tenantId, string sourceDocument, CancellationToken ct = default)
    {
        var client = _indexClient.GetSearchClient(_searchOptions.IndexName);

        var options = new SearchOptions
        {
            Filter = $"{TenantFilter(tenantId)} and sourceDocument eq '{EscapeOData(sourceDocument)}'",
            Size = MaxDeleteFetch
        };
        options.Select.Add("id");

        var response = await client.SearchAsync<KnowledgeChunkDocument>("*", options, ct);

        var ids = new List<string>();
        await foreach (var result in response.Value.GetResultsAsync())
            ids.Add(result.Document.Id);

        if (ids.Count == 0)
            return;

        await client.DeleteDocumentsAsync("id", ids, cancellationToken: ct);
        _logger.LogInformation(
            "Deleted {Count} chunks for tenant {TenantId} from {Source}.",
            ids.Count, tenantId, sourceDocument);
    }

    private static string TenantFilter(Guid tenantId) => $"tenantId eq '{tenantId}'";

    // OData string literals escape single quotes by doubling them.
    private static string EscapeOData(string value) => value.Replace("'", "''");

    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (_indexEnsured)
            return;

        await _indexLock.WaitAsync(ct);
        try
        {
            if (_indexEnsured)
                return;

            var index = BuildIndex(_searchOptions.IndexName);
            await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
            _indexEnsured = true;
            _logger.LogInformation("Ensured Azure AI Search index '{Index}'.", _searchOptions.IndexName);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private SearchIndex BuildIndex(string name)
    {
        var fields = new List<SearchField>
        {
            new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
            new SimpleField("tenantId", SearchFieldDataType.String) { IsFilterable = true },
            new SearchableField("content"),
            new SearchField(VectorField, SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = _embeddingDimensions,
                VectorSearchProfileName = VectorProfile
            },
            new SimpleField("sourceDocument", SearchFieldDataType.String) { IsFilterable = true },
            new SimpleField("metadata", SearchFieldDataType.String)
        };

        return new SearchIndex(name, fields)
        {
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile(VectorProfile, HnswConfig) },
                Algorithms = { new HnswAlgorithmConfiguration(HnswConfig) }
            }
        };
    }
}
