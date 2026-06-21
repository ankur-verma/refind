using Cortex.Database;
using Cortex.Modules.Search.Entities;
using Cortex.Infrastructure.AI;
using Cortex.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Modules.Search.UseCases;

public record TrackInteractionRequest(
    Guid? ContentItemId,
    string InteractionType,
    int DurationSeconds,
    string? SearchQuery
);

public class TrackInteractionUseCase
{
    private readonly CortexDbContext _context;
    private readonly IPythonAIService _pythonAiService;

    public TrackInteractionUseCase(CortexDbContext context, IPythonAIService pythonAiService)
    {
        _context = context;
        _pythonAiService = pythonAiService;
    }

    public async Task<Result<bool>> ExecuteAsync(Guid userId, TrackInteractionRequest request, CancellationToken ct = default)
    {
        var interaction = new UserInteraction
        {
            UserId = userId,
            ContentItemId = request.ContentItemId,
            InteractionType = request.InteractionType,
            DurationSeconds = request.DurationSeconds,
            SearchQuery = request.SearchQuery,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserInteractions.Add(interaction);
        await _context.SaveChangesAsync(ct);

        // Asynchronously update user profile in Python service without blocking the API call
        _ = Task.Run(async () =>
        {
            try
            {
                await _pythonAiService.UpdateUserProfileAsync(userId, CancellationToken.None);
            }
            catch
            {
                // Silence background exceptions to prevent worker crash
            }
        });

        return Result.Success(true);
    }
}
