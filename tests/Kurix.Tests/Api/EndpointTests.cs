using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.Auth;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Kurix.Tests.Api;

public class EndpointTests(KurixApiFactory factory) : IClassFixture<KurixApiFactory>
{
    private record SeedResult(Guid TenantId, string ApiKey, string Email, string Password);

    private SeedResult Seed()
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<KurixDbContext>();
        var apiKeyHasher = sp.GetRequiredService<IApiKeyHasher>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher>();

        var apiKey = apiKeyHasher.GenerateApiKey();
        var email = $"user-{Guid.NewGuid():N}@kurix.io";
        const string password = "Secret123!";

        var tenant = new Tenant
        {
            Name = "IT Tenant",
            ApiKeyHash = apiKeyHasher.Hash(apiKey),
            Status = TenantStatus.Active,
            SettingsJson = new TenantSettings().ToJson()
        };
        db.Tenants.Add(tenant);
        db.DashboardUsers.Add(new DashboardUser
        {
            TenantId = tenant.Id,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = DashboardRole.Owner
        });
        db.SaveChanges();

        return new SeedResult(tenant.Id, apiKey, email, password);
    }

    private async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginDto>();
        return body!.Token;
    }

    private record LoginDto(string Token, string Email, string Role, Guid TenantId);
    private record ChatDto(string Reply, bool Escalated, string SessionId, Guid ConversationId, string[] ToolsUsed);

    [Fact]
    public async Task Health_Is_Public()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chat_Without_ApiKey_Is_Unauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { message = "hola" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_With_Valid_ApiKey_Returns_Reply()
    {
        var seed = Seed();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", seed.ApiKey);

        var response = await client.PostAsJsonAsync("/api/chat", new { message = "hola" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatDto>();
        Assert.Equal("Respuesta de prueba.", body!.Reply);
        Assert.False(string.IsNullOrEmpty(body.SessionId));
        Assert.NotEqual(Guid.Empty, body.ConversationId);
    }

    [Fact]
    public async Task Chat_With_Invalid_ApiKey_Is_Unauthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "kx_invalid");

        var response = await client.PostAsJsonAsync("/api/chat", new { message = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Is_Unauthorized()
    {
        var seed = Seed();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = seed.Email, password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Endpoint_Requires_Jwt()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/conversations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Then_Access_Metrics_And_Conversations()
    {
        var seed = Seed();
        var client = factory.CreateClient();
        var token = await LoginAsync(client, seed.Email, seed.Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var metrics = await client.GetAsync("/api/metrics");
        Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);

        var conversations = await client.GetAsync("/api/conversations");
        Assert.Equal(HttpStatusCode.OK, conversations.StatusCode);
    }

    [Fact]
    public async Task Ingest_Document_Then_List_It()
    {
        var seed = Seed();
        var client = factory.CreateClient();
        var token = await LoginAsync(client, seed.Email, seed.Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ingest = await client.PostAsJsonAsync("/api/knowledge/documents",
            new { fileName = "faq.txt", content = "Horario de atención: 9 a 18." });
        Assert.Equal(HttpStatusCode.OK, ingest.StatusCode);

        var list = await client.GetFromJsonAsync<DocumentDto[]>("/api/knowledge/documents");
        Assert.Contains(list!, d => d.FileName == "faq.txt" && d.Status == "Ingested");
    }

    private record DocumentDto(Guid Id, string FileName, string Status, int ChunkCount);

    [Fact]
    public async Task Get_And_Update_Tenant_Settings()
    {
        var seed = Seed();
        var client = factory.CreateClient();
        var token = await LoginAsync(client, seed.Email, seed.Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var update = await client.PutAsJsonAsync("/api/tenant/settings", new
        {
            persona = "Persona actualizada",
            enabledTools = new[] { "search_inventory" },
            fallbackMessage = "Mensaje de respaldo"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var settings = await client.GetFromJsonAsync<SettingsDto>("/api/tenant/settings");
        Assert.Equal("Persona actualizada", settings!.Persona);
        Assert.NotNull(settings.EnabledTools);
        Assert.Equal(["search_inventory"], settings.EnabledTools);
    }

    private record SettingsDto(string Persona, string[]? EnabledTools, string? EscalationWebhookUrl, string FallbackMessage);
}
