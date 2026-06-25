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


