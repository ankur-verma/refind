using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Search.Services;
using Cortex.Shared;

namespace Cortex.Modules.Search.UseCases;

public class RefreshMindsetUseCase
{
    private readonly IUserMindsetService _mindsetService;

    public RefreshMindsetUseCase(IUserMindsetService mindsetService)
    {
        _mindsetService = mindsetService;
    }

    public async Task<Result<UserMindset>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var mindset = await _mindsetService.CompileMindsetAsync(userId, ct);
        return Result.Success(mindset);
    }
}
