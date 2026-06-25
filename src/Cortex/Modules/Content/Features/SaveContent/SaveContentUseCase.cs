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
/// Ingests a URL: detects platform via IContentSource,
/// saves metadata, and publishes to AI queue for async processing.
/// </summary>
public class SaveContentUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly IEnumerable<Cortex.Modules.Content.Domain.IContentSource> _sources;
    private readonly IMessageBroker _messageBroker;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;

    public SaveContentUseCase(
        IContentItemRepository contentRepo,
        IEnumerable<Cortex.Modules.Content.Domain.IContentSource> sources,
        IMessageBroker messageBroker,
        ICacheService cache,
        IUnitOfWork unitOfWork)
    {
        _contentRepo = contentRepo;
        _sources = sources;
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

        // Detect platform type from URL via IContentSource
        var source = _sources.FirstOrDefault(s => s.CanHandle(request.Url)) ?? _sources.First(s => s.PlatformType == PlatformType.Web);
        var platformType = source.PlatformType;

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
}
