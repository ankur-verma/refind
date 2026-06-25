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

        // Pipeline & Sources
        services.AddScoped<ContentIngestionPipeline>();
        
        // Smart Collection Engine
        services.AddSingleton<Cortex.Modules.Content.Domain.Rules.IRulesEngine, Cortex.Modules.Content.Domain.Rules.RulesEngine>();
        services.AddScoped<ICollectionMembershipEngine, CollectionMembershipEngine>();
        services.AddScoped<ICollectionGenerator, CollectionGenerator>();
        
        // Proactive Memory Engine
        services.AddSingleton<IContextService, ContextService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IReminderService, ReminderService>();
        
        // Workers
        services.AddHostedService<CollectionGeneratorWorker>();
        services.AddHostedService<MemoryLifecycleWorker>();
        services.AddHostedService<GraphSyncWorker>();
        
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.YouTubeSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.InstagramSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.TikTokSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.VideoSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.ArticleSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.WebPageSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.PDFSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.ShoppingSource>();
        services.AddHttpClient<Cortex.Modules.Content.Domain.IContentSource, Cortex.Modules.Content.Services.Sources.MapsSource>();

        // Social import services
        services.AddHttpClient<YouTubeImportService>();

        return services;
    }
}
