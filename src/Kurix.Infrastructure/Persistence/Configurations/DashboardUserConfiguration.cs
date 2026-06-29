using Kurix.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurix.Infrastructure.Persistence.Configurations;

public class DashboardUserConfiguration : IEntityTypeConfiguration<DashboardUser>
{
    public void Configure(EntityTypeBuilder<DashboardUser> builder)
    {
        builder.ToTable("DashboardUsers");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>();

        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.DashboardUsers)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Email is the login identifier and must be unique per tenant.
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
    }
}
