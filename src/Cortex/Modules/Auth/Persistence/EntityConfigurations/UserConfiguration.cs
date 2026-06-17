using Cortex.Modules.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Auth.Persistence.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(255);
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(255);
        builder.Property(x => x.PushToken).HasMaxLength(255);
        builder.HasIndex(x => x.RefreshTokenHash).HasDatabaseName("idx_users_refreshtokenhash");

        builder.HasMany(x => x.Identities).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Subscription).WithOne(x => x.User).HasForeignKey<Subscription>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Mindset).WithOne(x => x.User).HasForeignKey<UserMindset>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ContentItems).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.DripTracks).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
