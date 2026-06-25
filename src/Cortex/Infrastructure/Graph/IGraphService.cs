using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Infrastructure.Graph;

public interface IGraphService
{
    Task UpsertUserAsync(string userId, string email, CancellationToken ct = default);
    Task UpsertTopicAsync(string topicName, CancellationToken ct = default);
    Task UpsertLocationAsync(string locationName, CancellationToken ct = default);
    Task UpsertContentItemAsync(string contentId, string title, string userId, CancellationToken ct = default);
    
    // Edges
    Task LinkUserToTopicAsync(string userId, string topicName, string relation = "INTERESTED_IN", int weight = 1, CancellationToken ct = default);
    Task LinkUserToContentAsync(string userId, string contentId, CancellationToken ct = default);
    Task LinkContentToLocationAsync(string contentId, string locationName, CancellationToken ct = default);
    Task LinkContentToTopicAsync(string contentId, string topicName, CancellationToken ct = default);

    // Queries
    Task<List<string>> GetRecommendationsForUserAsync(string userId, CancellationToken ct = default);
}
