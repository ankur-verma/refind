using Cortex.Modules.Drip.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Drip.Persistence.EntityConfigurations;

public class DripStepConfiguration : IEntityTypeConfiguration<DripStep>
{
    public void Configure(EntityTypeBuilder<DripStep> builder)
    {
        builder.ToTable("DripSteps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskTitle).HasMaxLength(255);
        builder.Property(x => x.IsCompleted).HasDefaultValue(false);
        builder.HasIndex(x => x.DripTrackId).HasDatabaseName("idx_dripsteps_trackid");
    }
}
