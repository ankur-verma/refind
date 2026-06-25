using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Search.Persistence.EntityConfigurations;

public class UserBehaviorEventConfiguration : IEntityTypeConfiguration<UserBehaviorEvent>
{
    public void Configure(EntityTypeBuilder<UserBehaviorEvent> builder)
    {
        builder.ToTable("UserBehaviorEvents");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EventType).HasConversion<string>().IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ContentItem)
            .WithMany()
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
