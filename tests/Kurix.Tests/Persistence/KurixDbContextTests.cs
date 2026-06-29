using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Tests.Persistence;

/// <summary>
/// Milestone 1 smoke tests: the EF Core model is valid and tenant-owned
/// entities persist and round-trip with their relationships intact.
/// </summary>
public class KurixDbContextTests
{
    private static KurixDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<KurixDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    [Fact]
    public void Model_Builds_With_All_Expected_Entities()
    {
        using var ctx = NewContext(Guid.NewGuid().ToString());

        var entities = ctx.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToHashSet();

        Assert.Contains(nameof(Tenant), entities);
        Assert.Contains(nameof(Conversation), entities);
        Assert.Contains(nameof(Message), entities);
        Assert.Contains(nameof(KnowledgeDocument), entities);
        Assert.Contains(nameof(DashboardUser), entities);
    }

    [Fact]
    public async Task Tenant_With_Conversation_And_Messages_RoundTrips()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();

        await using (var ctx = NewContext(dbName))
        {
            ctx.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Demo PYME",
                ApiKeyHash = "hash",
                Status = TenantStatus.Active
            });
            ctx.Conversations.Add(new Conversation
            {
                TenantId = tenantId,
                SessionId = "sess-1",
                Messages =
                {
                    new Message { Role = MessageRole.User, Content = "Hola" },
                    new Message { Role = MessageRole.Assistant, Content = "¡Hola! ¿En qué puedo ayudarte?" }
                }
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext(dbName))
        {
            var tenant = await ctx.Tenants.FirstAsync(t => t.Id == tenantId);
            Assert.Equal("Demo PYME", tenant.Name);
            Assert.Equal(TenantStatus.Active, tenant.Status);

            var conversation = await ctx.Conversations
                .Include(c => c.Messages)
                .SingleAsync(c => c.TenantId == tenantId);

            Assert.Equal("sess-1", conversation.SessionId);
            Assert.Equal(ConversationStatus.Active, conversation.Status);
            Assert.Equal(2, conversation.Messages.Count);
        }
    }
}
