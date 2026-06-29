using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.Escalation;
using Kurix.Core.MultiTenancy;
using Kurix.Infrastructure.Escalation;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kurix.Tests.Escalation;

public class EscalationServiceTests
{
    // No webhook is configured in these tests, so the HTTP client is never used.
    private sealed class UnusedHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            throw new InvalidOperationException("HTTP client should not be created when no webhook is set.");
    }

    private static KurixDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<KurixDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task Escalate_Flips_Conversation_Status_To_Escalated()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await using (var ctx = NewContext(dbName))
        {
            ctx.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "T",
                ApiKeyHash = "h",
                SettingsJson = new TenantSettings().ToJson() // no webhook
            });
            ctx.Conversations.Add(new Conversation
            {
                Id = conversationId,
                TenantId = tenantId,
                SessionId = "sess-1",
                Status = ConversationStatus.Active
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext(dbName))
        {
            var service = new EscalationService(
                ctx, new UnusedHttpClientFactory(), NullLogger<EscalationService>.Instance);

            var result = await service.EscalateAsync(
                tenantId, conversationId, new EscalationRequest("motivo", null));

            Assert.False(result.WebhookDelivered);
        }

        await using (var ctx = NewContext(dbName))
        {
            var conversation = await ctx.Conversations.SingleAsync(c => c.Id == conversationId);
            Assert.Equal(ConversationStatus.Escalated, conversation.Status);
        }
    }

    [Fact]
    public async Task Escalate_Returns_False_When_Conversation_Missing()
    {
        await using var ctx = NewContext(Guid.NewGuid().ToString());
        var service = new EscalationService(
            ctx, new UnusedHttpClientFactory(), NullLogger<EscalationService>.Instance);

        var result = await service.EscalateAsync(
            Guid.NewGuid(), Guid.NewGuid(), new EscalationRequest("motivo", null));

        Assert.False(result.WebhookDelivered);
    }
}
