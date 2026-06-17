using Cortex.Modules.Content.Controllers;
using Cortex.Modules.Content.DTOs;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Content.Services;
using Cortex.Modules.Content.UseCases;
using Cortex.Modules.Content.Validators;
using Cortex.Infrastructure.Media;
using Cortex.Infrastructure.Settings;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Modules.Content;

public static class ContentModule
{
    public static IServiceCollection AddContentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<MinioSettings>(configuration.GetSection("Minio"));
        services.Configure<LocalExtractionSettings>(configuration.GetSection("LocalExtraction"));

        // Local media extraction (yt-dlp + Whisper — free, zero-cost pipeline)
        services.AddSingleton<LocalVideoExtractor>();
        services.AddSingleton<LocalAudioTranscriber>();

        // Use cases
        services.AddScoped<SaveContentUseCase>();
        services.AddScoped<GetContentFeedUseCase>();
        services.AddScoped<GetContentDetailUseCase>();
        services.AddScoped<DeclutterContentUseCase>();
        services.AddScoped<DeleteContentUseCase>();
        services.AddScoped<ReprocessContentUseCase>();

        // Validators
        services.AddScoped<IValidator<SaveContentRequest>, SaveContentValidator>();

        // Repositories
        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IContentPayloadRepository, ContentPayloadRepository>();
        services.AddScoped<IActionItemRepository, ActionItemRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        // Processors & Factory
        services.AddHttpClient<YouTubeContentProcessor>();
        services.AddHttpClient<WebPageContentProcessor>();
        services.AddHttpClient<InstagramContentProcessor>();
        services.AddHttpClient<PdfContentProcessor>();
        services.AddHttpClient<TikTokContentProcessor>();
        services.AddHttpClient<VideoContentProcessor>();

        services.AddScoped<IContentProcessorFactory, ContentProcessorFactory>();

        // Social import services
        services.AddHttpClient<YouTubeImportService>();

        return services;
    }
}
