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
