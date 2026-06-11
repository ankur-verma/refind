using Cortex.Application.DTOs.Auth;
using Cortex.Application.Interfaces.Repositories;
using Cortex.Application.Interfaces.Services;
using Cortex.Domain.Entities;
using Cortex.Infrastructure.Settings;
using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Cortex.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    private readonly IUserRepository _users;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly GoogleOAuthSettings _googleSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        ISubscriptionRepository subscriptions,
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtOptions,
        IOptions<GoogleOAuthSettings> googleOptions,
        HttpClient httpClient,
        ILogger<AuthService> logger)
    {
        _users = users;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtOptions.Value;
        _googleSettings = googleOptions.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var validationError = ValidateRegistration(request);
        if (validationError is not null)
            return Result.Failure<AuthResponse>(validationError);

        var email = NormalizeEmail(request.Email);
        if (await _users.ExistsAsync(email, ct))
            return Result.Failure<AuthResponse>("An account already exists for this email.");

        var refreshToken = GenerateRefreshToken();
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RefreshTokenHash = HashRefreshToken(refreshToken),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            LastActiveAt = DateTime.UtcNow
        };

        user.Identities.Add(new UserIdentity
        {
            UserId = user.Id,
            AuthProvider = AuthProvider.Local,
            ProviderId = email
        });

        await _users.AddAsync(user, ct);
        await _subscriptions.AddAsync(new Subscription
        {
            UserId = user.Id,
            Tier = SubscriptionTier.Free,
            IsActive = true,
            StartDate = DateTime.UtcNow
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(CreateAuthResponse(user, refreshToken));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result.Failure<AuthResponse>("Email and password are required.");

        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), ct);
        if (user?.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>("Invalid email or password.");

        var refreshToken = RotateRefreshToken(user);
        user.LastActiveAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(CreateAuthResponse(user, refreshToken));
    }

    public async Task<Result<AuthResponse>> OAuthLoginAsync(OAuthRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return Result.Failure<AuthResponse>("OAuth ID token is required.");

        var identity = await ValidateOAuthIdentityAsync(request, ct);
        if (identity is null)
            return Result.Failure<AuthResponse>("OAuth token validation failed.");

        var user = await _users.GetByIdentityAsync(request.Provider, identity.ProviderId, ct);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(identity.Email))
                return Result.Failure<AuthResponse>("OAuth token did not include an email address.");

            var email = NormalizeEmail(identity.Email);
            user = await _users.GetByEmailAsync(email, ct);

            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    LastActiveAt = DateTime.UtcNow
                };
                user.Identities.Add(new UserIdentity
                {
                    UserId = user.Id,
                    AuthProvider = request.Provider,
                    ProviderId = identity.ProviderId
                });

                await _users.AddAsync(user, ct);
                await _subscriptions.AddAsync(new Subscription
                {
                    UserId = user.Id,
                    Tier = SubscriptionTier.Free,
                    IsActive = true,
                    StartDate = DateTime.UtcNow
                }, ct);
            }
            else if (!user.Identities.Any(i => i.AuthProvider == request.Provider && i.ProviderId == identity.ProviderId))
            {
                user.Identities.Add(new UserIdentity
                {
                    UserId = user.Id,
                    AuthProvider = request.Provider,
                    ProviderId = identity.ProviderId
                });
            }
        }

        var refreshToken = RotateRefreshToken(user);
        user.LastActiveAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(CreateAuthResponse(user, refreshToken));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Failure<AuthResponse>("Refresh token is required.");

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(refreshTokenHash, ct);
        if (user is null)
            return Result.Failure<AuthResponse>("Invalid or expired refresh token.");

        var nextRefreshToken = RotateRefreshToken(user);
        user.LastActiveAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(CreateAuthResponse(user, nextRefreshToken));
    }

    private async Task<OAuthIdentity?> ValidateOAuthIdentityAsync(OAuthRequest request, CancellationToken ct)
    {
        return request.Provider switch
        {
            AuthProvider.Google => await ValidateOpenIdTokenAsync(
                request.IdToken,
                "https://www.googleapis.com/oauth2/v3/certs",
                new[] { "https://accounts.google.com", "accounts.google.com" },
                _googleSettings.ClientId,
                ct),
            AuthProvider.Apple => await ValidateOpenIdTokenAsync(
                request.IdToken,
                "https://appleid.apple.com/auth/keys",
                new[] { "https://appleid.apple.com" },
                null,
                ct),
            _ => null
        };
    }

    private async Task<OAuthIdentity?> ValidateOpenIdTokenAsync(
        string idToken,
        string jwksUrl,
        string[] issuers,
        string? audience,
        CancellationToken ct)
    {
        try
        {
            var jwksJson = await _httpClient.GetStringAsync(jwksUrl, ct);
            var keys = new JsonWebKeySet(jwksJson).Keys;
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = issuers,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = TokenHandler.ValidateToken(idToken, validationParameters, out _);
            var providerId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(providerId))
                return null;

            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? principal.FindFirstValue(ClaimTypes.Email);
            return new OAuthIdentity(providerId, email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OAuth ID token validation failed");
            return null;
        }
    }

    private AuthResponse CreateAuthResponse(User user, string refreshToken)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            AccessToken = TokenHandler.WriteToken(token),
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Tier = user.Subscription?.Tier ?? SubscriptionTier.Free
        };
    }

    private string RotateRefreshToken(User user)
    {
        var refreshToken = GenerateRefreshToken();
        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        return refreshToken;
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes.ToArray());
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }

    private static string? ValidateRegistration(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required.";
        if (!request.Email.Contains('@', StringComparison.Ordinal) || !request.Email.Contains('.', StringComparison.Ordinal))
            return "Invalid email format.";
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return "Password must be at least 8 characters.";
        if (!request.Password.Any(char.IsUpper) || !request.Password.Any(char.IsDigit))
            return "Password must contain at least one uppercase letter and one digit.";
        if (request.Password != request.ConfirmPassword)
            return "Passwords do not match.";
        return null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private sealed record OAuthIdentity(string ProviderId, string? Email);
}
