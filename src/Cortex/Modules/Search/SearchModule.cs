using Cortex.Modules.Search.UseCases;
using Cortex.Modules.Search.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Modules.Search;

public static class SearchModule
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Services
        services.AddScoped<IUserMindsetService, UserMindsetService>();
        services.AddScoped<IInterestScoringService, InterestScoringService>();
        services.AddScoped<IInterestTrendAnalyzer, InterestTrendAnalyzer>();
        services.AddScoped<IInterestEngine, InterestEngine>();
        services.AddScoped<IRecommendationService, RecommendationService>();

        // Use cases
        services.AddScoped<SemanticSearchUseCase>();
        services.AddScoped<GetMindsetUseCase>();
        services.AddScoped<RefreshMindsetUseCase>();
        services.AddScoped<UpdateMindsetUseCase>();
        services.AddScoped<RAGQueryUseCase>();
        services.AddScoped<TrackInteractionUseCase>();
        services.AddScoped<GetInterestProfileUseCase>();
        
        services.AddScoped<Cortex.Modules.Search.Persistence.IChatRepository, Cortex.Modules.Search.Persistence.ChatRepository>();
        services.AddScoped<GlobalChatUseCase>();

        return services;
    }
}
