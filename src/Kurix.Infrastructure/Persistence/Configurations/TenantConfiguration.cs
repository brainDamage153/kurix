using Kurix.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurix.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(256).IsRequired();
        builder.Property(t => t.ApiKeyHash).HasMaxLength(256).IsRequired();
        builder.Property(t => t.SettingsJson).IsRequired();
        builder.Property(t => t.Status).HasConversion<int>();

        // API key lookups happen on every widget request: must be indexed and unique.
        builder.HasIndex(t => t.ApiKeyHash).IsUnique();
    }
}
