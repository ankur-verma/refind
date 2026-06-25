using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Modules.Search.Entities;

namespace Cortex.Modules.Search.Persistence.EntityConfigurations;

public class UserMLProfileConfiguration : IEntityTypeConfiguration<UserMLProfile>
{
    public void Configure(EntityTypeBuilder<UserMLProfile> builder)
    {
        builder.ToTable("UserMLProfiles");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
               .WithOne()
               .HasForeignKey<UserMLProfile>(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // We assume 1536 dimensions if using Gemini Embedding, but can also be 384 for sentence-transformers
        // To be flexible, we can just use vector without a dimension limit if supported, or fix it to 384 since we'll use sentence-transformers.
        // The user mentioned sentence-transformers in Python, which often produces 384-d vectors (e.g., all-MiniLM-L6-v2).
        builder.Property(x => x.UserEmbedding)
               .HasColumnType("vector(384)");
    }
}
