using Cortex.Modules.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Auth.Persistence.EntityConfigurations;

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("UserIdentities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthProvider).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ProviderId).HasMaxLength(255);
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_useridentities_userid");
        builder.HasIndex(x => new { x.AuthProvider, x.ProviderId }).IsUnique().HasDatabaseName("uq_useridentities_provider_providerid");
    }
}
