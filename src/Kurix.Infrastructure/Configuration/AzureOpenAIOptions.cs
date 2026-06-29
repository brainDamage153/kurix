namespace Kurix.Infrastructure.Configuration;

/// <summary>
/// Binds the <c>AzureOpenAI</c> configuration section. Secrets (the API key)
/// must come from User Secrets in dev and Key Vault / App Configuration in prod.
/// </summary>
public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Deployment name for the chat model (e.g. gpt-4o-mini).</summary>
    public string ChatDeployment { get; set; } = "gpt-4o-mini";

    /// <summary>Deployment name for embeddings (e.g. text-embedding-3-small).</summary>
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";

    /// <summary>Vector dimensions produced by the embedding deployment.</summary>
    public int EmbeddingDimensions { get; set; } = 1536;
}
