using Cortex.Application.UseCases.Auth;
using Cortex.Application.UseCases.Content;
using Cortex.Application.UseCases.Drip;
using Cortex.Application.UseCases.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<OAuthLoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();

        services.AddScoped<SaveContentUseCase>();
        services.AddScoped<GetContentFeedUseCase>();
        services.AddScoped<GetContentDetailUseCase>();
        services.AddScoped<DeclutterContentUseCase>();
        services.AddScoped<DeleteContentUseCase>();

        services.AddScoped<CreateDripTrackUseCase>();
        services.AddScoped<AdvanceDripStepUseCase>();
        services.AddScoped<GetDripTracksUseCase>();

        services.AddScoped<SemanticSearchUseCase>();

        return services;
    }
}
