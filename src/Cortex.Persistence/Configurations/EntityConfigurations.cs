using Cortex.Domain.Entities;
using Cortex.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(255);
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(255);
        builder.Property(x => x.PushToken).HasMaxLength(255);
        builder.HasIndex(x => x.RefreshTokenHash).HasDatabaseName("idx_users_refreshtokenhash");

        builder.HasMany(x => x.Identities).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Subscription).WithOne(x => x.User).HasForeignKey<Subscription>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ContentItems).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.DripTracks).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("UserIdentities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthProvider).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ProviderId).HasMaxLength(255);
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_useridentities_userid");
        builder.HasIndex(x => new { x.AuthProvider, x.ProviderId }).IsUnique().HasDatabaseName("uq_useridentities_provider_providerid");
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tier).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

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

public class ContentPayloadConfiguration : IEntityTypeConfiguration<ContentPayload>
{
    public void Configure(EntityTypeBuilder<ContentPayload> builder)
    {
        builder.ToTable("ContentPayloads");
        builder.HasKey(x => x.ContentItemId);
        builder.Property(x => x.SemanticEmbedding).HasColumnType("vector(1536)");

        // HNSW vector index for rapid similarity search
        builder.HasIndex(x => x.SemanticEmbedding)
            .HasDatabaseName("idx_contentpayloads_embedding")
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}

public class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
{
    public void Configure(EntityTypeBuilder<ActionItem> builder)
    {
        builder.ToTable("ActionItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.IsCompleted).HasDefaultValue(false);
        builder.HasIndex(x => x.ContentItemId).HasDatabaseName("idx_actionitems_contentitemid");
    }
}

public class DripTrackConfiguration : IEntityTypeConfiguration<DripTrack>
{
    public void Configure(EntityTypeBuilder<DripTrack> builder)
    {
        builder.ToTable("DripTracks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasMany(x => x.Steps).WithOne(x => x.DripTrack).HasForeignKey(x => x.DripTrackId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DripStepConfiguration : IEntityTypeConfiguration<DripStep>
{
    public void Configure(EntityTypeBuilder<DripStep> builder)
    {
        builder.ToTable("DripSteps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskTitle).HasMaxLength(255);
        builder.Property(x => x.IsCompleted).HasDefaultValue(false);
        builder.HasIndex(x => x.DripTrackId).HasDatabaseName("idx_dripsteps_trackid");
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class ContentItemTagConfiguration : IEntityTypeConfiguration<ContentItemTag>
{
    public void Configure(EntityTypeBuilder<ContentItemTag> builder)
    {
        builder.ToTable("ContentItemTags");
        builder.HasKey(x => new { x.ContentItemId, x.TagId });
        builder.HasOne(x => x.ContentItem).WithMany(x => x.ContentItemTags).HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Tag).WithMany(x => x.ContentItemTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
