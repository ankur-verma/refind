using Cortex.Modules.Auth.Controllers;
using Cortex.Modules.Auth.DTOs;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Auth.Persistence;
using Cortex.Modules.Auth.Services;
using Cortex.Modules.Auth.UseCases;
using Cortex.Modules.Auth.Validators;
using Cortex.Infrastructure.Settings;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Modules.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<AuthSettings>(configuration.GetSection("Auth"));
        services.Configure<GoogleOAuthSettings>(configuration.GetSection("GoogleOAuth"));
        services.Configure<NotificationSettings>(configuration.GetSection("NotificationGateways"));

        // Use cases
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<OAuthLoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();

        // Validators
        services.AddScoped<IValidator<RegisterRequest>, RegisterUserValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();

        // Persistence Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUserActivityLogRepository, UserActivityLogRepository>();

        // Services (AuthService and NotificationService with DI integration)
        services.AddScoped<INotificationService, Cortex.Infrastructure.Notifications.NotificationService>();
        services.AddHttpClient<IAuthService, AuthService>();

        return services;
    }
}
