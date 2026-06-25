using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Drip.Entities;
using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Database;

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
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentPayload> ContentPayloads => Set<ContentPayload>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<DripTrack> DripTracks => Set<DripTrack>();
    public DbSet<DripStep> DripSteps => Set<DripStep>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContentItemTag> ContentItemTags => Set<ContentItemTag>();
    public DbSet<UserMindset> UserMindsets => Set<UserMindset>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserInteraction> UserInteractions => Set<UserInteraction>();
    public DbSet<UserInterest> UserInterests => Set<UserInterest>();
    public DbSet<InterestHistory> InterestHistories => Set<InterestHistory>();
    public DbSet<UserBehaviorEvent> UserBehaviorEvents => Set<UserBehaviorEvent>();
    public DbSet<UserMLProfile> UserMLProfiles => Set<UserMLProfile>();
    public DbSet<UserIntent> UserIntents => Set<UserIntent>();
    public DbSet<VideoSegment> VideoSegments => Set<VideoSegment>();
    public DbSet<AutoCollection> AutoCollections => Set<AutoCollection>();
    public DbSet<CollectionMembership> CollectionMemberships => Set<CollectionMembership>();
    public DbSet<ContentInsight> ContentInsights => Set<ContentInsight>();
    public DbSet<SemanticEntity> SemanticEntities => Set<SemanticEntity>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<UserRecommendation> UserRecommendations => Set<UserRecommendation>();
    public DbSet<ProactiveMemory> ProactiveMemories => Set<ProactiveMemory>();
    
    // Smart Collections (FE-14)
    public DbSet<SmartCollection> SmartCollections => Set<SmartCollection>();
    public DbSet<SmartCollectionItem> SmartCollectionItems => Set<SmartCollectionItem>();
    public DbSet<SmartCollectionSignal> SmartCollectionSignals => Set<SmartCollectionSignal>();
    
    // Memory UX (FE-14)
    public DbSet<MemoryTimelineEvent> MemoryTimelineEvents => Set<MemoryTimelineEvent>();
    public DbSet<MemoryRecommendation> MemoryRecommendations => Set<MemoryRecommendation>();

    // Join tables
    public DbSet<ContentInsightEntity> ContentInsightEntities => Set<ContentInsightEntity>();
    public DbSet<ContentInsightLocation> ContentInsightLocations => Set<ContentInsightLocation>();
    public DbSet<ContentInsightProduct> ContentInsightProducts => Set<ContentInsightProduct>();
    public DbSet<ContentInsightBrand> ContentInsightBrands => Set<ContentInsightBrand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");



        // Apply all IEntityTypeConfiguration implementations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CortexDbContext).Assembly);
    }
}
