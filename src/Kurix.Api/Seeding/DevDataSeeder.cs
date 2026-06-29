using Kurix.Core.Auth;
using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Api.Seeding;

/// <summary>
/// Development-only helper: applies migrations and seeds one demo tenant plus a
/// dashboard user, printing the generated widget API key and login credentials so
/// the API, widget and dashboard are immediately usable locally. Best-effort —
/// a missing/unreachable database is logged, not fatal.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            var db = sp.GetRequiredService<KurixDbContext>();
            await db.Database.MigrateAsync();

            if (await db.Tenants.AnyAsync())
                return;

            var apiKeyHasher = sp.GetRequiredService<IApiKeyHasher>();
            var passwordHasher = sp.GetRequiredService<IPasswordHasher>();

            var apiKey = apiKeyHasher.GenerateApiKey();
            const string email = "demo@kurix.io";
            const string password = "Demo1234!";

            var tenant = new Tenant
            {
                Name = "Demo PYME",
                ApiKeyHash = apiKeyHasher.Hash(apiKey),
                Status = TenantStatus.Active,
                SettingsJson = new TenantSettings
                {
                    Persona = "Sos el asistente de atención al cliente de Demo PYME. " +
                              "Respondé en español, con tono cordial y profesional."
                }.ToJson()
            };
            db.Tenants.Add(tenant);

            db.DashboardUsers.Add(new DashboardUser
            {
                TenantId = tenant.Id,
                Email = email,
                PasswordHash = passwordHasher.Hash(password),
                Role = DashboardRole.Owner
            });

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Seeded demo tenant.\n  Widget API key: {ApiKey}\n  Dashboard login: {Email} / {Password}",
                apiKey, email, password);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dev seeding skipped (database unavailable?).");
        }
    }
}
