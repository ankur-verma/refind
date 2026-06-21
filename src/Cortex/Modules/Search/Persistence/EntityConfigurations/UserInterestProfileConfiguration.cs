using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Search.Persistence.EntityConfigurations;

public class UserInterestProfileConfiguration : IEntityTypeConfiguration<UserInterestProfile>
{
    public void Configure(EntityTypeBuilder<UserInterestProfile> builder)
    {
        builder.ToTable("UserInterestProfiles");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Trend).IsRequired().HasMaxLength(50);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
