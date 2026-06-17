using Cortex.Modules.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Auth.Persistence.EntityConfigurations;

public class UserMindsetConfiguration : IEntityTypeConfiguration<UserMindset>
{
    public void Configure(EntityTypeBuilder<UserMindset> builder)
    {
        builder.ToTable("UserMindsets");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.FocusAreasJson)
            .HasMaxLength(2000)
            .IsRequired();
            
        builder.Property(x => x.ConsumptionPreference)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(x => x.NarrativeSummary)
            .HasMaxLength(4000)
            .IsRequired();
            
        builder.Property(x => x.LastUpdated)
            .IsRequired();

        // 1:1 relationship with User
        builder.HasOne(x => x.User)
            .WithOne(x => x.Mindset)
            .HasForeignKey<UserMindset>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
