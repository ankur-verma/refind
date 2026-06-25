using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class ProactiveMemoryConfiguration : IEntityTypeConfiguration<ProactiveMemory>
{
    public void Configure(EntityTypeBuilder<ProactiveMemory> builder)
    {
        builder.ToTable("ProactiveMemories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MemoryType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ActionUrl)
            .HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
