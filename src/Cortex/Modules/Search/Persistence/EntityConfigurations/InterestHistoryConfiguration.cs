using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Search.Persistence.EntityConfigurations;

public class InterestHistoryConfiguration : IEntityTypeConfiguration<InterestHistory>
{
    public void Configure(EntityTypeBuilder<InterestHistory> builder)
    {
        builder.ToTable("InterestHistories");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Reason).HasMaxLength(200);

        builder.HasOne(x => x.UserInterest)
            .WithMany()
            .HasForeignKey(x => x.UserInterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
