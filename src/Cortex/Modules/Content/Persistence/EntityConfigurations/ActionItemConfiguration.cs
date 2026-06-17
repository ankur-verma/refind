using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

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
