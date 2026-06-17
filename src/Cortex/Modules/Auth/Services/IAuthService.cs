using Cortex.Modules.Auth.DTOs;
using Cortex.Shared;

namespace Cortex.Modules.Auth.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> OAuthLoginAsync(OAuthRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);
    Task<Result<bool>> SendPhoneOtpAsync(SendPhoneCodeRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> VerifyPhoneOtpAsync(VerifyPhoneRequest request, CancellationToken ct = default);
}
