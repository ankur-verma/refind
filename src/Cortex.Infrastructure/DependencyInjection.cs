using Cortex.Application.Interfaces.Factories;
using Cortex.Application.Interfaces.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Auth;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.ContentProcessing;
using Cortex.Infrastructure.ContentProcessing.Factories;
using Cortex.Infrastructure.Messaging;
using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Cortex.Infrastructure;

/// <summary>
/// DI registration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.Configure<GoogleOAuthSettings>(configuration.GetSection("GoogleOAuth"));
        services.Configure<MinioSettings>(configuration.GetSection("Minio"));
        services.Configure<AIServiceSettings>(configuration.GetSection("AIService"));

        services.AddHttpClient<IAuthService, AuthService>();
        services.AddHttpClient<IAIExtractionService, AIExtractionService>();
        services.AddHttpClient<YouTubeContentProcessor>();
        services.AddHttpClient<WebPageContentProcessor>();
        services.AddHttpClient<InstagramContentProcessor>();
        services.AddHttpClient<PdfContentProcessor>();
        services.AddHttpClient<TikTokContentProcessor>();

        // Redis
        var redisSettings = configuration.GetSection("Redis").Get<RedisSettings>() ?? new RedisSettings();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // RabbitMQ
        var rabbitSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
        var factory = new ConnectionFactory
        {
            HostName = rabbitSettings.HostName,
            Port = rabbitSettings.Port,
            UserName = rabbitSettings.UserName,
            Password = rabbitSettings.Password
        };
        services.AddSingleton<IConnectionFactory>(factory);
        services.AddScoped<IMessageBroker, RabbitMqMessageBroker>();

        // Factory Pattern registrations
        services.AddScoped<IContentProcessorFactory, ContentProcessorFactory>();

        return services;
    }
}
