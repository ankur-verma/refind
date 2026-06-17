using Cortex.Modules.Content.DTOs;
using Cortex.Modules.Content.Services;
using Cortex.Modules.Auth.Persistence;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Drip.Persistence;
using Cortex.Database;
using Cortex.Modules.Auth.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Drip.Entities;
using Cortex.Shared;
using Cortex.Shared.Enums;
using Cortex.Shared.Exceptions;

namespace Cortex.Modules.Content.UseCases;

/// <summary>
/// Ingests a URL: detects platform, delegates to factory-created processor,
/// saves metadata, and publishes to AI queue for async processing.
/// </summary>
public class SaveContentUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly IContentProcessorFactory _processorFactory;
    private readonly IMessageBroker _messageBroker;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;

    public SaveContentUseCase(
        IContentItemRepository contentRepo,
        IContentProcessorFactory processorFactory,
        IMessageBroker messageBroker,
        ICacheService cache,
        IUnitOfWork unitOfWork)
    {
        _contentRepo = contentRepo;
        _processorFactory = processorFactory;
        _messageBroker = messageBroker;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> ExecuteAsync(Guid userId, SaveContentRequest request, CancellationToken ct = default)
    {
        Guard.AgainstNullOrEmpty(request.Url, nameof(request.Url));
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<Guid>("Invalid URL format.");
        }

        // Check for duplicate
        if (await _contentRepo.ExistsByUrlAsync(userId, request.Url, ct))
            return Result.Failure<Guid>("DUPLICATE_URL");

        // Detect platform type from URL
        var platformType = DetectPlatformType(request.Url);

        // Create content item in Processing state
        var contentItem = new ContentItem
        {
            UserId = userId,
            OriginalUrl = request.Url,
            PlatformType = platformType,
            Status = ContentStatus.Processing,
            Title = "Processing..."
        };

        await _contentRepo.AddAsync(contentItem, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Publish to AI extraction queue for async processing
        await _messageBroker.PublishAsync("ai_extraction_queue", new
        {
            ContentItemId = contentItem.Id,
            UserId = userId,
            Url = request.Url,
            PlatformType = platformType.ToString()
        }, ct);

        await _cache.RemoveByPrefixAsync($"feed:{userId}:", ct);

        return Result.Success(contentItem.Id);
    }

    private static PlatformType DetectPlatformType(string url)
    {
        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.ToLowerInvariant();

        if (host.Contains("youtube.com") || host.Contains("youtu.be"))
            return PlatformType.YouTube;
        if (host.Contains("instagram.com"))
            return PlatformType.Instagram;
        if (host.Contains("tiktok.com"))
            return PlatformType.TikTok;
        if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return PlatformType.PDF;

        if (path.EndsWith(".mp4") || path.EndsWith(".mov") || path.EndsWith(".webm") || path.EndsWith(".avi") || 
            path.EndsWith(".mkv") || path.EndsWith(".flv") || path.EndsWith(".mp3") || path.EndsWith(".wav") || 
            path.EndsWith(".m4a") || path.EndsWith(".ogg") ||
            host.Contains("vimeo.com") || host.Contains("dailymotion.com") || host.Contains("twitch.tv") || 
            host.Contains("fb.watch") || host.Contains("facebook.com/watch") || host.Contains("loom.com") || 
            host.Contains("wistia.com") || host.Contains("rumble.com") || host.Contains("bilibili.com"))
        {
            return PlatformType.Video;
        }

        return PlatformType.Web;
    }
}

/// <summary>
/// Returns mood-filtered content feed for the Cognitive Dashboard.
/// </summary>
public class GetContentFeedUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly ICacheService _cache;

    public GetContentFeedUseCase(IContentItemRepository contentRepo, ICacheService cache)
    {
        _contentRepo = contentRepo;
        _cache = cache;
    }

    public async Task<PagedList<ContentItem>> ExecuteAsync(Guid userId, ContentFeedQuery query, CancellationToken ct = default)
    {
        var cacheKey = $"feed:{userId}:{query.EnergyLevel}:{query.PlatformType}:{query.Page}";
        var cached = await _cache.GetAsync<PagedList<ContentItem>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await _contentRepo.GetFeedAsync(
            userId, query.EnergyLevel, query.PlatformType, query.Page, query.PageSize, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}

/// <summary>
/// Returns full content detail with payload (summary, raw text, action items).
/// </summary>
public class GetContentDetailUseCase
{
    private readonly IContentItemRepository _contentRepo;

    public GetContentDetailUseCase(IContentItemRepository contentRepo)
    {
        _contentRepo = contentRepo;
    }

    public async Task<ContentItem> ExecuteAsync(Guid contentItemId, CancellationToken ct = default)
    {
        var item = await _contentRepo.GetByIdWithPayloadAsync(contentItemId, ct);
        if (item is null)
            throw new NotFoundException(nameof(ContentItem), contentItemId);
        return item;
    }
}

/// <summary>
/// Handles the "Swipe to Clean" declutter actions (Pin, Archive, Delete).
/// </summary>
public class DeclutterContentUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;

    public DeclutterContentUseCase(IContentItemRepository contentRepo, ICacheService cache, IUnitOfWork unitOfWork)
    {
        _contentRepo = contentRepo;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid userId, DeclutterActionRequest request, CancellationToken ct = default)
    {
        var item = await _contentRepo.GetByIdAsync(request.ContentItemId, ct);
        if (item is null || item.UserId != userId)
            return Result.Failure("Content item not found.");

        switch (request.Action)
        {
            case DeclutterAction.Pin:
                item.IsPinned = true;
                item.LastReviewedAt = DateTime.UtcNow;
                break;
            case DeclutterAction.Archive:
                item.IsArchived = true;
                item.LastReviewedAt = DateTime.UtcNow;
                break;
            case DeclutterAction.Delete:
                await _contentRepo.DeleteAsync(item, ct);
                await _cache.RemoveByPrefixAsync($"feed:{userId}:", ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result.Success();
        }

        await _contentRepo.UpdateAsync(item, ct);
        await _cache.RemoveByPrefixAsync($"feed:{userId}:", ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>
/// Deletes a content item and all related data (cascading).
/// </summary>
public class DeleteContentUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteContentUseCase(IContentItemRepository contentRepo, ICacheService cache, IUnitOfWork unitOfWork)
    {
        _contentRepo = contentRepo;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid userId, Guid contentItemId, CancellationToken ct = default)
    {
        var item = await _contentRepo.GetByIdAsync(contentItemId, ct);
        if (item is null || item.UserId != userId)
            return Result.Failure("Content item not found.");

        await _contentRepo.DeleteAsync(item, ct);
        await _cache.RemoveByPrefixAsync($"feed:{userId}:", ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ReprocessContentUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly IMessageBroker _messageBroker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public ReprocessContentUseCase(
        IContentItemRepository contentRepo,
        IMessageBroker messageBroker,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _contentRepo = contentRepo;
        _messageBroker = messageBroker;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result> ExecuteAsync(Guid userId, Guid contentItemId, CancellationToken ct = default)
    {
        var item = await _contentRepo.GetByIdAsync(contentItemId, ct);
        if (item is null || item.UserId != userId)
            return Result.Failure("Content item not found.");

        item.Status = ContentStatus.Processing;
        await _contentRepo.UpdateAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _messageBroker.PublishAsync("ai_extraction_queue", new
        {
            ContentItemId = item.Id,
            UserId = userId,
            Url = item.OriginalUrl,
            PlatformType = item.PlatformType.ToString()
        }, ct);

        await _cache.RemoveByPrefixAsync($"feed:{userId}:", ct);

        return Result.Success();
    }
}
