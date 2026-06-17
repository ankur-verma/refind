using Cortex.Modules.Auth.Entities;
using Cortex.Database;
using Cortex.Modules.Search.Services;
using Cortex.Shared;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Search.UseCases;

public class GetMindsetUseCase
{
    private readonly CortexDbContext _context;
    private readonly IUserMindsetService _mindsetService;

    public GetMindsetUseCase(CortexDbContext context, IUserMindsetService mindsetService)
    {
        _context = context;
        _mindsetService = mindsetService;
    }

    public async Task<Result<UserMindset>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var mindset = await _context.UserMindsets.FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted, ct);
        if (mindset is null)
        {
            mindset = await _mindsetService.CompileMindsetAsync(userId, ct);
        }

        return Result.Success(mindset);
    }
}
