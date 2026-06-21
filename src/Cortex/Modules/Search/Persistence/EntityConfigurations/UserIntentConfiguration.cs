using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Search.Persistence.EntityConfigurations;

public class UserIntentConfiguration : IEntityTypeConfiguration<UserIntent>
{
    public void Configure(EntityTypeBuilder<UserIntent> builder)
    {
        builder.ToTable("UserIntents");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.GoalDescription).IsRequired().HasMaxLength(250);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
