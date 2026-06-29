using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Configuration;
using Kurix.Infrastructure.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurix.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. The API project calls
/// <see cref="AddInfrastructure"/> to register persistence and external-service
/// clients. Later milestones extend this with Azure OpenAI / AI Search clients
/// and repositories; for Milestone 1 it wires EF Core and options binding.
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

        services.AddDbContext<KurixDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Sql"),
                sql => sql.MigrationsAssembly(typeof(KurixDbContext).Assembly.FullName)));

        // Multi-tenancy: deterministic API-key hashing, tenant lookups, and the
        // request-scoped tenant context populated by the resolution middleware.
        services.AddSingleton<IApiKeyHasher, Sha256ApiKeyHasher>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantContext, TenantContext>();

        return services;
    }
}
