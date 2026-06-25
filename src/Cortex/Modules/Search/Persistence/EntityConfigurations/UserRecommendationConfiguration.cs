using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Search.Persistence.EntityConfigurations;

public class UserRecommendationConfiguration : IEntityTypeConfiguration<UserRecommendation>
{
    public void Configure(EntityTypeBuilder<UserRecommendation> builder)
    {
        builder.ToTable("UserRecommendations");

        builder.HasKey(e => e.Id);
        
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.RecommendationType);

        builder.Property(e => e.RecommendationType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(e => e.Title)
            .HasMaxLength(500);
            
        builder.Property(e => e.Reason)
            .HasMaxLength(1000);
    }
}
