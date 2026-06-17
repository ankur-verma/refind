using Cortex.Modules.Auth.DTOs;
using Cortex.Shared;
using Cortex.Modules.Auth.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Modules.Auth.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserUseCase _registerUseCase;
    private readonly LoginUserUseCase _loginUseCase;
    private readonly OAuthLoginUseCase _oauthUseCase;
    private readonly RefreshTokenUseCase _refreshUseCase;

    public AuthController(
        RegisterUserUseCase registerUseCase,
        LoginUserUseCase loginUseCase,
        OAuthLoginUseCase oauthUseCase,
        RefreshTokenUseCase refreshUseCase)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
        _oauthUseCase = oauthUseCase;
        _refreshUseCase = refreshUseCase;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _registerUseCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<AuthResponse>.Fail(result.Error));
        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _loginUseCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Error));
        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    [HttpPost("oauth")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuth([FromBody] OAuthRequest request, CancellationToken ct)
    {
        var result = await _oauthUseCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<AuthResponse>.Fail(result.Error));
        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _refreshUseCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Error));
        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, [FromServices] VerifyEmailUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<AuthResponse>.Fail(result.Error));
        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }

    [HttpPost("phone/send-code")]
    [AllowAnonymous]
    public async Task<IActionResult> SendPhoneCode([FromBody] SendPhoneCodeRequest request, [FromServices] SendPhoneOtpUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<bool>.Fail(result.Error));
        return Ok(ApiResponse<bool>.Ok(result.Value));
    }

    [HttpPost("phone/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request, [FromServices] VerifyPhoneOtpUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<AuthResponse>.Fail(result.Error));
        return Ok(ApiResponse<AuthResponse>.Ok(result.Value));
    }
}
