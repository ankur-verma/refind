using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

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
