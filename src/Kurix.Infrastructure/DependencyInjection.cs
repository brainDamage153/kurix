using Azure;
using Azure.Search.Documents.Indexes;
using Kurix.Core.Knowledge;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Configuration;
using Kurix.Infrastructure.Knowledge;
using Kurix.Infrastructure.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kurix.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. The API project calls
/// <see cref="AddInfrastructure"/> to register persistence, multi-tenancy and the
/// RAG / external-service clients.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureOpenAIOptions>(
            configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<AzureAISearchOptions>(
            configuration.GetSection(AzureAISearchOptions.SectionName));
        services.Configure<RagOptions>(
            configuration.GetSection(RagOptions.SectionName));

        services.AddDbContext<KurixDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Sql"),
                sql => sql.MigrationsAssembly(typeof(KurixDbContext).Assembly.FullName)));

        // Multi-tenancy: deterministic API-key hashing, tenant lookups, and the
        // request-scoped tenant context populated by the resolution middleware.
        services.AddSingleton<IApiKeyHasher, Sha256ApiKeyHasher>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantContext, TenantContext>();

        // RAG: Azure AI Search index client, embeddings, token chunker and the
        // knowledge service that ties them together. Singletons because the
        // clients are thread-safe and the knowledge service caches index state.
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureAISearchOptions>>().Value;
            return new SearchIndexClient(new Uri(opts.Endpoint), new AzureKeyCredential(opts.ApiKey));
        });
        services.AddSingleton<ITextChunker, TokenTextChunker>();
        services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();
        services.AddSingleton<IKnowledgeService, AzureAISearchKnowledgeService>();

        return services;
    }
}
