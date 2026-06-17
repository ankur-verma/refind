using Cortex.Modules.Drip.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Drip.Persistence.EntityConfigurations;

public class DripTrackConfiguration : IEntityTypeConfiguration<DripTrack>
{
    public void Configure(EntityTypeBuilder<DripTrack> builder)
    {
        builder.ToTable("DripTracks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.HasMany(x => x.Steps).WithOne(x => x.DripTrack).HasForeignKey(x => x.DripTrackId).OnDelete(DeleteBehavior.Cascade);
    }
}
