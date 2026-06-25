using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class CollectionMembershipConfiguration : IEntityTypeConfiguration<CollectionMembership>
{
    public void Configure(EntityTypeBuilder<CollectionMembership> builder)
    {
        builder.ToTable("CollectionMemberships");
        builder.HasKey(x => new { x.AutoCollectionId, x.ContentItemId });

        builder.HasOne(x => x.AutoCollection)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.AutoCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ContentItem)
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
