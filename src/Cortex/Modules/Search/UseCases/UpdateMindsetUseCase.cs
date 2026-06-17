using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Auth.Entities;
using Cortex.Database;
using Cortex.Modules.Search.DTOs;
using Cortex.Shared;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Search.UseCases;

public class UpdateMindsetUseCase
{
    private readonly CortexDbContext _context;

    public UpdateMindsetUseCase(CortexDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserMindset>> ExecuteAsync(Guid userId, UpdateMindsetRequest request, CancellationToken ct = default)
    {
        var mindset = await _context.UserMindsets.FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted, ct);
        if (mindset is null)
        {
            mindset = new UserMindset { UserId = userId };
            await _context.UserMindsets.AddAsync(mindset, ct);
        }

        mindset.FocusAreasJson = JsonSerializer.Serialize(request.FocusAreas ?? new List<string>());
        mindset.ConsumptionPreference = request.ConsumptionPreference ?? "Concise";
        mindset.NarrativeSummary = request.NarrativeSummary ?? string.Empty;
        mindset.LastUpdated = DateTime.UtcNow;

        _context.UserMindsets.Update(mindset);
        await _context.SaveChangesAsync(ct);

        return Result.Success(mindset);
    }
}
