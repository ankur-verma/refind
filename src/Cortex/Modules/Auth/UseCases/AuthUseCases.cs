using Cortex.Modules.Auth.DTOs;
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

namespace Cortex.Modules.Auth.UseCases;

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

public class VerifyEmailUseCase
{
    private readonly IAuthService _authService;

    public VerifyEmailUseCase(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        return await _authService.VerifyEmailAsync(request, ct);
    }
}

public class SendPhoneOtpUseCase
{
    private readonly IAuthService _authService;

    public SendPhoneOtpUseCase(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<bool>> ExecuteAsync(SendPhoneCodeRequest request, CancellationToken ct = default)
    {
        return await _authService.SendPhoneOtpAsync(request, ct);
    }
}

public class VerifyPhoneOtpUseCase
{
    private readonly IAuthService _authService;

    public VerifyPhoneOtpUseCase(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(VerifyPhoneRequest request, CancellationToken ct = default)
    {
        return await _authService.VerifyPhoneOtpAsync(request, ct);
    }
}
