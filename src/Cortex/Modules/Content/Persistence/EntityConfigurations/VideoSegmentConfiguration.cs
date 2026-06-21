using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class VideoSegmentConfiguration : IEntityTypeConfiguration<VideoSegment>
{
    public void Configure(EntityTypeBuilder<VideoSegment> builder)
    {
        builder.ToTable("VideoSegments");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);

        builder.HasOne(x => x.ContentItem)
            .WithMany(x => x.VideoSegments)
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
