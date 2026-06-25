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
