using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class VideoSegmentConfiguration : IEntityTypeConfiguration<VideoSegment>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<VideoSegment> builder)
    {
        builder.ToTable("VideoSegments");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);

        builder.Property(v => v.Summary)
            .HasMaxLength(2000);

        builder.Property(v => v.SegmentType)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("General");

        builder.Property(v => v.Metadata)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<VideoSegmentMetadata>(v, _jsonOptions) ?? new VideoSegmentMetadata()
            );

        builder.HasOne(x => x.ContentItem)
            .WithMany(x => x.VideoSegments)
            .HasForeignKey(x => x.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
