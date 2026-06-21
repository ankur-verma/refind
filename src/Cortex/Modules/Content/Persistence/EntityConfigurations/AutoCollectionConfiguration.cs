using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class AutoCollectionConfiguration : IEntityTypeConfiguration<AutoCollection>
{
    public void Configure(EntityTypeBuilder<AutoCollection> builder)
    {
        builder.ToTable("AutoCollections");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UserIntent)
            .WithMany()
            .HasForeignKey(x => x.UserIntentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
