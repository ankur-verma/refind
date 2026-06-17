using Cortex.Modules.Content.Entities;
using Cortex.Modules.Drip.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("ContentItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalUrl).IsRequired();
        builder.Property(x => x.PlatformType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.EnergyLevel).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.IsArchived).HasDefaultValue(false);
        builder.Property(x => x.IsPinned).HasDefaultValue(false);

        // Unique constraint: prevent duplicate saves per user
        builder.HasIndex(x => new { x.UserId, x.OriginalUrl })
            .IsUnique()
            .HasDatabaseName("uq_user_url");

        // Composite index for mood-filtered dashboard feed
        builder.HasIndex(x => new { x.UserId, x.EnergyLevel, x.IsArchived, x.CreatedAt })
            .HasDatabaseName("idx_contentitems_feed");

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_contentitems_userid");

        builder.HasOne(x => x.Payload).WithOne(x => x.ContentItem).HasForeignKey<ContentPayload>(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ActionItems).WithOne(x => x.ContentItem).HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.DripTracks).WithOne(x => x.ContentItem).HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
