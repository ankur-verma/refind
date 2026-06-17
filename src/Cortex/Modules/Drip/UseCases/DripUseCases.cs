using Cortex.Modules.Drip.DTOs;
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

namespace Cortex.Modules.Drip.UseCases;

/// <summary>
/// Creates a new Drip Track by slicing content into daily micro-tasks.
/// </summary>
public class CreateDripTrackUseCase
{
    private readonly IDripTrackRepository _trackRepo;
    private readonly IDripStepRepository _stepRepo;
    private readonly IContentItemRepository _contentRepo;
    private readonly IContentPayloadRepository _payloadRepo;
    private readonly IAIExtractionService _aiService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDripTrackUseCase(
        IDripTrackRepository trackRepo,
        IDripStepRepository stepRepo,
        IContentItemRepository contentRepo,
        IContentPayloadRepository payloadRepo,
        IAIExtractionService aiService,
        IUnitOfWork unitOfWork)
    {
        _trackRepo = trackRepo;
        _stepRepo = stepRepo;
        _contentRepo = contentRepo;
        _payloadRepo = payloadRepo;
        _aiService = aiService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> ExecuteAsync(Guid userId, CreateDripTrackRequest request, CancellationToken ct = default)
    {
        var contentItem = await _contentRepo.GetByIdAsync(request.ContentItemId, ct);
        if (contentItem is null || contentItem.UserId != userId)
            return Result.Failure<Guid>("Content item not found.");

        var totalDays = Math.Clamp(request.TotalDays, 1, 30);
        var payload = await _payloadRepo.GetByContentItemIdAsync(request.ContentItemId, ct);
        var sourceText = FirstNonEmpty(payload?.RawText, payload?.QuickSparkSummary, contentItem.Title);
        var actions = await _aiService.ExtractActionsAsync(sourceText, ct);

        var track = new DripTrack
        {
            UserId = userId,
            ContentItemId = request.ContentItemId,
            TotalDays = totalDays,
            CurrentDay = 1,
            Status = DripTrackStatus.Active
        };

        await _trackRepo.AddAsync(track, ct);
        await _stepRepo.AddRangeAsync(CreateSteps(track.Id, totalDays, contentItem.Title, payload?.QuickSparkSummary, sourceText, actions), ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(track.Id);
    }

    private static List<DripStep> CreateSteps(
        Guid trackId,
        int totalDays,
        string contentTitle,
        string? summary,
        string sourceText,
        List<(string Description, string Type, int Order)> actions)
    {
        var stepBodies = actions
            .OrderBy(action => action.Order)
            .Select(action => action.Description)
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .ToList();

        if (stepBodies.Count == 0)
            stepBodies = sourceText
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(sentence => sentence.Length > 20)
                .Take(totalDays)
                .ToList();

        if (stepBodies.Count == 0)
            stepBodies.Add(FirstNonEmpty(summary, $"Review {contentTitle} and write down one useful takeaway."));

        var steps = new List<DripStep>();
        for (var day = 1; day <= totalDays; day++)
        {
            var body = stepBodies[(day - 1) % stepBodies.Count];
            steps.Add(new DripStep
            {
                DripTrackId = trackId,
                DayNumber = day,
                TaskTitle = $"Day {day}: {CreateTitle(body)}",
                TaskDescription = body,
                ScheduledFor = DateTime.UtcNow.Date.AddDays(day - 1)
            });
        }

        return steps;
    }

    private static string CreateTitle(string description)
    {
        var title = description.Trim();
        if (title.Length > 70)
            title = title[..70].TrimEnd() + "...";
        return title;
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}

/// <summary>
/// Marks a drip step as completed and advances the track.
/// </summary>
public class AdvanceDripStepUseCase
{
    private readonly IDripStepRepository _stepRepo;
    private readonly IDripTrackRepository _trackRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AdvanceDripStepUseCase(IDripStepRepository stepRepo, IDripTrackRepository trackRepo, IUnitOfWork unitOfWork)
    {
        _stepRepo = stepRepo;
        _trackRepo = trackRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid userId, Guid stepId, CancellationToken ct = default)
    {
        var step = await _stepRepo.GetByIdAsync(stepId, ct);
        if (step is null)
            return Result.Failure("Drip step not found.");

        var track = await _trackRepo.GetByIdAsync(step.DripTrackId, ct);
        if (track is null || track.UserId != userId)
            return Result.Failure("Drip step not found.");

        step.IsCompleted = true;
        await _stepRepo.UpdateAsync(step, ct);

        track.CurrentDay = Math.Min(track.CurrentDay + 1, track.TotalDays);
        if (track.CurrentDay >= track.TotalDays)
            track.Status = DripTrackStatus.Completed;
        await _trackRepo.UpdateAsync(track, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>
/// Retrieves all drip tracks for a user.
/// </summary>
public class GetDripTracksUseCase
{
    private readonly IDripTrackRepository _trackRepo;

    public GetDripTracksUseCase(IDripTrackRepository trackRepo)
    {
        _trackRepo = trackRepo;
    }

    public async Task<List<DripTrack>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        return await _trackRepo.GetByUserIdAsync(userId, ct);
    }
}
