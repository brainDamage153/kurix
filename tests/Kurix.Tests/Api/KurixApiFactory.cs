using Kurix.Core.Conversations;
using Kurix.Core.Knowledge;
using Kurix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurix.Tests.Api;

/// <summary>
/// Spins up the API in-process with an in-memory database and fakes for the
/// Azure-backed services (chat + knowledge), so endpoints, auth and wiring can be
/// tested without any external dependency.
/// </summary>
public class KurixApiFactory : WebApplicationFactory<Program>
{
    public const string JwtSigningKey = "integration-test-signing-key-0123456789!!";
    private readonly string _dbName = "it-" + Guid.NewGuid();

    public sealed class StubChat : IChatCompletionService
    {
        public Task<ChatCompletionResult> CompleteAsync(
            IReadOnlyList<LlmMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken ct) =>
            Task.FromResult(new ChatCompletionResult("Respuesta de prueba.", [], 3, 2));
    }

    public sealed class StubKnowledge : IKnowledgeService
    {
        public Task<KnowledgeIngestionResult> IngestAsync(Guid t, KnowledgeDocumentInput d, CancellationToken ct) =>
            Task.FromResult(new KnowledgeIngestionResult(2));
        public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(Guid t, string q, int k, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<KnowledgeSearchResult>>([]);
        public Task DeleteDocumentAsync(Guid t, string s, CancellationToken ct) => Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = JwtSigningKey,
                ["ConnectionStrings:Sql"] = "Server=unused;Database=unused;"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Swap SQL Server for an in-memory store shared across the factory.
            services.RemoveAll(typeof(DbContextOptions<KurixDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<KurixDbContext>));
            services.RemoveAll<KurixDbContext>();
            services.AddDbContext<KurixDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Replace Azure-backed services with deterministic fakes.
            services.RemoveAll<IChatCompletionService>();
            services.AddSingleton<IChatCompletionService, StubChat>();
            services.RemoveAll<IKnowledgeService>();
            services.AddSingleton<IKnowledgeService, StubKnowledge>();
        });
    }
}
