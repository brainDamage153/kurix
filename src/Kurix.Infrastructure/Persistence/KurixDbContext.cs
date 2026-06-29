using Kurix.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kurix.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Kurix relational store (Azure SQL). All tenant-owned
/// entities carry a <c>TenantId</c>; tenant isolation is enforced at the query
/// layer via <c>ITenantContext</c>, not by global filters alone, so that
/// background/ingestion paths remain explicit about the tenant they target.
/// </summary>
public class KurixDbContext(DbContextOptions<KurixDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<DashboardUser> DashboardUsers => Set<DashboardUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KurixDbContext).Assembly);
    }
}
