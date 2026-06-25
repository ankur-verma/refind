using Cortex.Modules.Content.Domain;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Content.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Settings;
using Cortex.Shared.Enums;
using Cortex.Modules.Drip.Persistence;
using Cortex.Modules.Drip.Entities;
using Cortex.Modules.Content.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services;

public class ContentIngestionPipeline
{
    private readonly IEnumerable<IContentSource> _sources;
    private readonly IContentItemRepository _contentRepo;
    private readonly IContentPayloadRepository _payloadRepo;
    private readonly IActionItemRepository _actionRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIExtractionService _aiService;
    private readonly IPythonAIService _pythonAiService;
    private readonly ICacheService _cache;
    private readonly IHubContext<ContentHub> _hubContext;
    private readonly CortexDbContext _dbContext;
    private readonly AIServiceSettings _aiSettings;
    private readonly ILogger<ContentIngestionPipeline> _logger;
    private readonly ICollectionMembershipEngine _membershipEngine;

    public ContentIngestionPipeline(
        IEnumerable<IContentSource> sources,
        IContentItemRepository contentRepo,
        IContentPayloadRepository payloadRepo,
        IActionItemRepository actionRepo,
        ITagRepository tagRepo,
        IUnitOfWork unitOfWork,
        IAIExtractionService aiService,
        IPythonAIService pythonAiService,
        ICacheService cache,
        IHubContext<ContentHub> hubContext,
        CortexDbContext dbContext,
        IOptions<AIServiceSettings> aiOptions,
        ILogger<ContentIngestionPipeline> logger,
        ICollectionMembershipEngine membershipEngine)
    {
        _sources = sources;
        _contentRepo = contentRepo;
        _payloadRepo = payloadRepo;
        _actionRepo = actionRepo;
        _tagRepo = tagRepo;
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _pythonAiService = pythonAiService;
        _cache = cache;
        _hubContext = hubContext;
        _dbContext = dbContext;
        _aiSettings = aiOptions.Value;
        _logger = logger;
        _membershipEngine = membershipEngine;
    }

    public async Task ProcessAsync(Guid contentItemId, Guid userId, string url, PlatformType platformType, CancellationToken ct)
    {
        var contentItem = await _contentRepo.GetByIdAsync(contentItemId, ct);
        if (contentItem is null) return;

        try
        {
            // 1. Find matching source
            var source = _sources.FirstOrDefault(s => s.PlatformType == platformType) 
                         ?? _sources.First(s => s.PlatformType == PlatformType.Web);

            // 2. Metadata, Raw Content, Media Extraction
            ContentExtractionResult result;
            bool isGated = false;
            try
            {
                result = await source.ExtractAsync(url, ct);

                if (string.IsNullOrWhiteSpace(result.Transcript) || result.Transcript.Length < 10)
                {
                    isGated = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Content extraction failed for URL: {Url}. Setting status to Failed.", url);
                contentItem.Status = ContentStatus.Failed;
                contentItem.Title = CreateTitleFromUrl(url);
                await _contentRepo.UpdateAsync(contentItem, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ContentStatusChanged", new { id = contentItem.Id, status = "Failed", title = contentItem.Title, url = contentItem.OriginalUrl }, ct);
                return;
            }

            var rawTextForAi = string.Join(". ", new[] { result.Title, result.Description, result.Transcript }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var estimatedMins = EstimateMinutes(rawTextForAi, fallback: 5);

            // 3. Update Content Metadata
            contentItem.Title = result.Title;
            contentItem.HeroImageUrl = result.Media.FirstOrDefault();
            contentItem.EnergyLevel = EnergyForMinutes(estimatedMins);
            contentItem.ConsumeTimeMins = estimatedMins;

            // 4. AI Classification
            string summary;
            float[] embedding;

            if (isGated)
            {
                summary = "Requires Manual Review.";
                embedding = await _aiService.GenerateEmbeddingAsync("Gated content requires manual review", ct);
            }
            else
            {
                summary = await _aiService.GenerateSummaryAsync(rawTextForAi, ct);
                embedding = await _aiService.GenerateEmbeddingAsync(rawTextForAi, ct);
            }

            // 5. Persistence
            var payload = await _payloadRepo.GetByContentItemIdAsync(contentItem.Id, ct);
            if (payload is null)
            {
                payload = new ContentPayload
                {
                    ContentItemId = contentItem.Id,
                    RawText = rawTextForAi,
                    QuickSparkSummary = summary
                };
                if (_aiSettings.ActiveProvider == "LocalOllama") payload.OllamaEmbedding = new Vector(embedding);
                else payload.GeminiEmbedding = new Vector(embedding);
                    
                await _payloadRepo.AddAsync(payload, ct);
            }
            else
            {
                payload.RawText = rawTextForAi;
                payload.QuickSparkSummary = summary;
                if (_aiSettings.ActiveProvider == "LocalOllama") payload.OllamaEmbedding = new Vector(embedding);
                else payload.GeminiEmbedding = new Vector(embedding);
                    
                await _payloadRepo.UpdateAsync(payload, ct);
            }

            // 5b. Content Understanding (Phase 3)
            if (!isGated)
            {
                var insightsData = await _aiService.ExtractInsightsAsync(rawTextForAi, ct);
                
                var insight = await _dbContext.ContentInsights
                    .Include(i => i.Entities)
                    .Include(i => i.Locations)
                    .Include(i => i.Products)
                    .Include(i => i.Brands)
                    .FirstOrDefaultAsync(i => i.ContentItemId == contentItem.Id, ct);
                
                if (insight == null)
                {
                    insight = new ContentInsight { ContentItemId = contentItem.Id };
                    _dbContext.ContentInsights.Add(insight);
                }
                
                insight.Category = insightsData.Category ?? "";
                insight.SubCategory = insightsData.SubCategory ?? "";
                insight.Intent = insightsData.Intent ?? "";
                insight.Sentiment = insightsData.Sentiment ?? "";
                insight.Topics = insightsData.Topics ?? new List<string>();
                
                await ProcessEntitiesAsync(insight, insightsData.Entities, EntityType.General, ct);
                await ProcessEntitiesAsync(insight, insightsData.People, EntityType.Person, ct);
                await ProcessEntitiesAsync(insight, insightsData.Events, EntityType.Event, ct);
                
                await ProcessLocationsAsync(insight, insightsData.Locations, ct);
                await ProcessProductsAsync(insight, insightsData.Products, ct);
                await ProcessBrandsAsync(insight, insightsData.Brands, ct);
                
                await _dbContext.SaveChangesAsync(ct);
            }

            // Extract Action Items
            if (!isGated)
            {
                var actions = await _aiService.ExtractActionsAsync(rawTextForAi, ct);
                var existingActions = await _actionRepo.GetByContentItemIdAsync(contentItem.Id, ct);
                if (existingActions.Count == 0)
                {
                    var actionItems = actions.Select(a => new ActionItem
                    {
                        ContentItemId = contentItem.Id,
                        Description = a.Description,
                        ItemType = a.Type == "Tool" ? ActionItemType.Tool : ActionItemType.Instruction,
                        SequenceOrder = a.Order
                    }).ToList();
                    await _actionRepo.AddRangeAsync(actionItems, ct);
                }
            }

            // Tags
            var existingTags = await _tagRepo.GetByContentItemIdAsync(contentItem.Id, ct);
            var existingTagNames = existingTags.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var tagName in result.Tags)
            {
                if (existingTagNames.Contains(tagName)) continue;

                var tag = await _tagRepo.GetByNameAsync(tagName, ct);
                if (tag is null)
                {
                    tag = new Tag { Name = tagName.ToLowerInvariant(), IsAIGenerated = true };
                    await _tagRepo.AddAsync(tag, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                await _tagRepo.AddContentItemTagAsync(contentItem.Id, tag.Id, ct);
            }

            // 6. Background Enrichment (Video segmentation)
            if (!isGated && (platformType == PlatformType.YouTube || platformType == PlatformType.Instagram || platformType == PlatformType.TikTok || platformType == PlatformType.Video))
            {
                try
                {
                    _logger.LogInformation("Requesting topic segments from Python AI service for video: {ContentItemId}", contentItem.Id);
                    var videoSegments = await _pythonAiService.AnalyzeVideoAsync(contentItem.Id, contentItem.OriginalUrl, null, rawTextForAi, ct);
                    if (videoSegments != null && videoSegments.Count > 0)
                    {
                        var segments = videoSegments.Select(s => new VideoSegment
                        {
                            ContentItemId = contentItem.Id,
                            StartSeconds = s.StartSeconds,
                            EndSeconds = s.EndSeconds,
                            Title = s.Title,
                            Summary = s.Summary,
                            SegmentType = s.SegmentType ?? "General",
                            Metadata = new VideoSegmentMetadata
                            {
                                Topics = s.Topics ?? new List<string>(),
                                Products = s.Products ?? new List<string>(),
                                Locations = s.Locations ?? new List<string>(),
                                Restaurants = s.Restaurants ?? new List<string>(),
                                Tips = s.Tips ?? new List<string>(),
                                Prices = s.Prices ?? new List<string>(),
                                Recommendations = s.Recommendations ?? new List<string>()
                            },
                            CreatedAt = DateTime.UtcNow
                        }).ToList();

                        _dbContext.VideoSegments.AddRange(segments);
                        await _dbContext.SaveChangesAsync(ct);
                        _logger.LogInformation("Saved {Count} video segments for content item {ContentItemId}", segments.Count, contentItem.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate video segments for content item {ContentItemId}", contentItem.Id);
                }
            }

            // Evaluate smart collection rules (Phase 4)
            try
            {
                // We use a fire-and-forget or await it depending on how fast it is.
                // It does a few DB queries, we can just await it since we're in a background job anyway.
                await _membershipEngine.EvaluateItemAsync(contentItem.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate item {ContentItemId} for collection membership", contentItem.Id);
            }

            // Finalize
            contentItem.Status = ContentStatus.Ready;
            await _contentRepo.UpdateAsync(contentItem, ct);
            await _cache.RemoveByPrefixAsync($"feed:{contentItem.UserId}:", ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ContentProcessed", new { id = contentItem.Id, status = 1 }, ct);

            _logger.LogInformation("Successfully processed content item {ContentItemId}", contentItem.Id);
        }
        catch (Exception ex)
        {
            contentItem.Status = ContentStatus.Failed;
            await _contentRepo.UpdateAsync(contentItem, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ContentProcessed", new { id = contentItem.Id, status = 2 }, ct);
            
            _logger.LogError(ex, "Failed to process content item {ContentItemId}", contentItem.Id);
            throw; // Re-throw to nack and dead-letter
        }
    }

    private async Task ProcessEntitiesAsync(ContentInsight insight, List<string> names, EntityType type, CancellationToken ct)
    {
        if (names == null) return;
        foreach (var n in names)
        {
            var cleanName = n?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(cleanName)) continue;

            var entity = await _dbContext.SemanticEntities.FirstOrDefaultAsync(e => e.Name.ToLower() == cleanName && e.Type == type, ct);
            if (entity == null)
            {
                entity = new SemanticEntity { Name = n!.Trim(), Type = type };
                _dbContext.SemanticEntities.Add(entity);
            }
            if (!insight.Entities.Any(e => e.SemanticEntityId == entity.Id || (e.SemanticEntity != null && e.SemanticEntity.Name.ToLowerInvariant() == cleanName)))
            {
                insight.Entities.Add(new ContentInsightEntity { SemanticEntity = entity });
            }
        }
    }

    private async Task ProcessLocationsAsync(ContentInsight insight, List<string> names, CancellationToken ct)
    {
        if (names == null) return;
        foreach (var n in names)
        {
            var cleanName = n?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(cleanName)) continue;

            var entity = await _dbContext.Locations.FirstOrDefaultAsync(e => e.Name.ToLower() == cleanName, ct);
            if (entity == null)
            {
                entity = new Location { Name = n!.Trim() };
                _dbContext.Locations.Add(entity);
            }
            if (!insight.Locations.Any(e => e.LocationId == entity.Id || (e.Location != null && e.Location.Name.ToLowerInvariant() == cleanName)))
            {
                insight.Locations.Add(new ContentInsightLocation { Location = entity });
            }
        }
    }

    private async Task ProcessProductsAsync(ContentInsight insight, List<string> names, CancellationToken ct)
    {
        if (names == null) return;
        foreach (var n in names)
        {
            var cleanName = n?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(cleanName)) continue;

            var entity = await _dbContext.Products.FirstOrDefaultAsync(e => e.Name.ToLower() == cleanName, ct);
            if (entity == null)
            {
                entity = new Product { Name = n!.Trim() };
                _dbContext.Products.Add(entity);
            }
            if (!insight.Products.Any(e => e.ProductId == entity.Id || (e.Product != null && e.Product.Name.ToLowerInvariant() == cleanName)))
            {
                insight.Products.Add(new ContentInsightProduct { Product = entity });
            }
        }
    }

    private async Task ProcessBrandsAsync(ContentInsight insight, List<string> names, CancellationToken ct)
    {
        if (names == null) return;
        foreach (var n in names)
        {
            var cleanName = n?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(cleanName)) continue;

            var entity = await _dbContext.Brands.FirstOrDefaultAsync(e => e.Name.ToLower() == cleanName, ct);
            if (entity == null)
            {
                entity = new Brand { Name = n!.Trim() };
                _dbContext.Brands.Add(entity);
            }
            if (!insight.Brands.Any(e => e.BrandId == entity.Id || (e.Brand != null && e.Brand.Name.ToLowerInvariant() == cleanName)))
            {
                insight.Brands.Add(new ContentInsightBrand { Brand = entity });
            }
        }
    }
}
