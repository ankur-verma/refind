using Cortex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Persistence;

/// <summary>
/// Main EF Core DbContext for Cortex.
/// Uses PostgreSQL with pgvector extension.
/// </summary>
public class CortexDbContext : DbContext
{
    public CortexDbContext(DbContextOptions<CortexDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentPayload> ContentPayloads => Set<ContentPayload>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<DripTrack> DripTracks => Set<DripTrack>();
    public DbSet<DripStep> DripSteps => Set<DripStep>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContentItemTag> ContentItemTags => Set<ContentItemTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Apply all IEntityTypeConfiguration implementations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CortexDbContext).Assembly);
    }
}
