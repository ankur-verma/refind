using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Search.Entities;

namespace Cortex.Modules.Search.Services;

public interface IRecommendationService
{
    Task GenerateAndStoreRecommendationsAsync(Guid userId, CancellationToken ct = default);
    Task<Dictionary<string, List<UserRecommendation>>> GetPersonalizedRecommendationsAsync(Guid userId, CancellationToken ct = default);
}
