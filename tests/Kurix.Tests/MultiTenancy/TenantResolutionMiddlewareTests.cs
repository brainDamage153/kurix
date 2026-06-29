using Kurix.Api.Middleware;
using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kurix.Tests.MultiTenancy;

public class TenantResolutionMiddlewareTests
{
    private readonly Sha256ApiKeyHasher _hasher = new();

    private sealed class FakeTenantRepository(Tenant? tenant) : ITenantRepository
    {
        public Task<Tenant?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken ct = default) =>
            Task.FromResult(tenant is not null && tenant.ApiKeyHash == apiKeyHash ? tenant : null);

        public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default) =>
            Task.FromResult(tenant?.Id == tenantId ? tenant : null);
    }

    private Tenant MakeTenant(string apiKey, TenantStatus status = TenantStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Demo PYME",
        ApiKeyHash = _hasher.Hash(apiKey),
        Status = status
    };

    private async Task<(bool nextCalled, int statusCode, TenantContext ctx)> RunAsync(
        Tenant? tenant, string? apiKeyHeader)
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<TenantResolutionMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        if (apiKeyHeader is not null)
            httpContext.Request.Headers[TenantResolutionMiddleware.ApiKeyHeader] = apiKeyHeader;

        var tenantContext = new TenantContext();
        await middleware.InvokeAsync(httpContext, tenantContext, new FakeTenantRepository(tenant), _hasher);

        return (nextCalled, httpContext.Response.StatusCode, tenantContext);
    }

    [Fact]
    public async Task No_Header_Passes_Through_Unresolved()
    {
        var (nextCalled, status, ctx) = await RunAsync(tenant: null, apiKeyHeader: null);

        Assert.True(nextCalled);
        Assert.False(ctx.IsResolved);
        Assert.Equal(StatusCodes.Status200OK, status);
    }

    [Fact]
    public async Task Valid_Key_Resolves_Tenant_And_Continues()
    {
        const string key = "kx_valid";
        var tenant = MakeTenant(key);

        var (nextCalled, _, ctx) = await RunAsync(tenant, key);

        Assert.True(nextCalled);
        Assert.True(ctx.IsResolved);
        Assert.Equal(tenant.Id, ctx.TenantId);
    }

    [Fact]
    public async Task Unknown_Key_Returns_401_And_Stops()
    {
        var tenant = MakeTenant("kx_valid");

        var (nextCalled, status, ctx) = await RunAsync(tenant, "kx_wrong");

        Assert.False(nextCalled);
        Assert.False(ctx.IsResolved);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
    }

    [Fact]
    public async Task Inactive_Tenant_Returns_401()
    {
        const string key = "kx_suspended";
        var tenant = MakeTenant(key, TenantStatus.Suspended);

        var (nextCalled, status, ctx) = await RunAsync(tenant, key);

        Assert.False(nextCalled);
        Assert.False(ctx.IsResolved);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
    }
}
