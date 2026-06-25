using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Cortex.Database.Configurations;

public class ContentInsightConfiguration : IEntityTypeConfiguration<ContentInsight>
{
    public void Configure(EntityTypeBuilder<ContentInsight> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasOne(e => e.ContentItem)
            .WithOne(c => c.Insight)
            .HasForeignKey<ContentInsight>(e => e.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Topics)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
    }
}

public class ContentInsightEntityConfiguration : IEntityTypeConfiguration<ContentInsightEntity>
{
    public void Configure(EntityTypeBuilder<ContentInsightEntity> builder)
    {
        builder.HasKey(e => new { e.ContentInsightId, e.SemanticEntityId });
        
        builder.HasOne(e => e.ContentInsight)
            .WithMany(c => c.Entities)
            .HasForeignKey(e => e.ContentInsightId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.SemanticEntity)
            .WithMany(s => s.Insights)
            .HasForeignKey(e => e.SemanticEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentInsightLocationConfiguration : IEntityTypeConfiguration<ContentInsightLocation>
{
    public void Configure(EntityTypeBuilder<ContentInsightLocation> builder)
    {
        builder.HasKey(e => new { e.ContentInsightId, e.LocationId });
        
        builder.HasOne(e => e.ContentInsight)
            .WithMany(c => c.Locations)
            .HasForeignKey(e => e.ContentInsightId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Location)
            .WithMany(s => s.Insights)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentInsightProductConfiguration : IEntityTypeConfiguration<ContentInsightProduct>
{
    public void Configure(EntityTypeBuilder<ContentInsightProduct> builder)
    {
        builder.HasKey(e => new { e.ContentInsightId, e.ProductId });
        
        builder.HasOne(e => e.ContentInsight)
            .WithMany(c => c.Products)
            .HasForeignKey(e => e.ContentInsightId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Product)
            .WithMany(s => s.Insights)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentInsightBrandConfiguration : IEntityTypeConfiguration<ContentInsightBrand>
{
    public void Configure(EntityTypeBuilder<ContentInsightBrand> builder)
    {
        builder.HasKey(e => new { e.ContentInsightId, e.BrandId });
        
        builder.HasOne(e => e.ContentInsight)
            .WithMany(c => c.Brands)
            .HasForeignKey(e => e.ContentInsightId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Brand)
            .WithMany(s => s.Insights)
            .HasForeignKey(e => e.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
