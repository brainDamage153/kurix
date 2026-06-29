using Kurix.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurix.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SessionId).HasMaxLength(128).IsRequired();
        builder.Property(c => c.Status).HasConversion<int>();

        builder.HasOne(c => c.Tenant)
            .WithMany(t => t.Conversations)
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tenant-scoped listing and session lookup.
        builder.HasIndex(c => new { c.TenantId, c.SessionId });
        builder.HasIndex(c => new { c.TenantId, c.Status });
    }
}
