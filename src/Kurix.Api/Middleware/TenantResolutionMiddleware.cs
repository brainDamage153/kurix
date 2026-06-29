using Kurix.Core.Enums;
using Kurix.Core.MultiTenancy;

namespace Kurix.Api.Middleware;

/// <summary>
/// Resolves the current tenant from the widget API key carried in a request
/// header and binds it into the request-scoped <see cref="ITenantContext"/>.
///
/// Behaviour:
/// <list type="bullet">
///   <item>No API-key header → request continues unresolved. Dashboard routes
///   authenticate with JWT and don't carry this header; endpoints that require a
///   tenant enforce it themselves.</item>
///   <item>Header present but key unknown or tenant not active → <c>401</c>.</item>
///   <item>Header present and valid → tenant bound into the context.</item>
/// </list>
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    /// <summary>Header the widget sends its API key in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantRepository tenantRepository,
        IApiKeyHasher apiKeyHasher)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValues))
        {
            await next(context);
            return;
        }

        var apiKey = apiKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await WriteUnauthorizedAsync(context, "Missing API key.");
            return;
        }

        var hash = apiKeyHasher.Hash(apiKey);
        var tenant = await tenantRepository.GetByApiKeyHashAsync(hash, context.RequestAborted);

        if (tenant is null)
        {
            logger.LogWarning("Tenant resolution failed: unknown API key.");
            await WriteUnauthorizedAsync(context, "Invalid API key.");
            return;
        }

        if (tenant.Status != TenantStatus.Active)
        {
            logger.LogWarning("Tenant {TenantId} is not active ({Status}).", tenant.Id, tenant.Status);
            await WriteUnauthorizedAsync(context, "Tenant is not active.");
            return;
        }

        tenantContext.SetTenant(tenant);
        await next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
