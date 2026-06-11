using Cortex.Application.DTOs.Auth;
using Cortex.Application.Interfaces.Repositories;
using Cortex.Application.Interfaces.Services;
using Cortex.Domain.Entities;
using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;

namespace Cortex.Application.UseCases.Auth;

public class RegisterUserUseCase
{
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionRepository _subRepo;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserUseCase(IUserRepository userRepo, ISubscriptionRepository subRepo, IAuthService authService, IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _subRepo = subRepo;
        _authService = authService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        return await _authService.RegisterAsync(request, ct);
    }
}

public class LoginUserUseCase
{
    private readonly IAuthService _authService;

    public LoginUserUseCase(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        return await _authService.LoginAsync(request, ct);
    }
}

public class OAuthLoginUseCase
{
    private readonly IAuthService _authService;

    public OAuthLoginUseCase(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(OAuthRequest request, CancellationToken ct = default)
    {
        return await _authService.OAuthLoginAsync(request, ct);
    }
}

public class RefreshTokenUseCase
{
    private readonly IAuthService _authService;

    public RefreshTokenUseCase(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        return await _authService.RefreshTokenAsync(request, ct);
    }
}
