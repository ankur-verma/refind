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
