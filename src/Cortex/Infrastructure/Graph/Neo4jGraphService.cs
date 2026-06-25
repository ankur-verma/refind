using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Cortex.Infrastructure.Graph;

public class Neo4jGraphService : IGraphService, IAsyncDisposable
{
    private readonly IDriver? _driver;
    private readonly ILogger<Neo4jGraphService> _logger;

    public Neo4jGraphService(IConfiguration config, ILogger<Neo4jGraphService> logger)
    {
        _logger = logger;
        
        var uri = config["Neo4j:Uri"];
        var user = config["Neo4j:Username"];
        var pass = config["Neo4j:Password"];

        if (!string.IsNullOrEmpty(uri) && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
        {
            try
            {
                _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Neo4j driver. Graph features will be disabled.");
            }
        }
    }

    public async Task UpsertUserAsync(string userId, string email, CancellationToken ct = default)
    {
        if (_driver == null) return;
        var query = "MERGE (u:User {id: $userId}) SET u.email = $email";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { userId, email });
        });
    }

    public async Task UpsertTopicAsync(string topicName, CancellationToken ct = default)
    {
        if (_driver == null || string.IsNullOrWhiteSpace(topicName)) return;
        var query = "MERGE (t:Topic {name: $topicName})";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { topicName = topicName.ToLowerInvariant() });
        });
    }

    public async Task UpsertLocationAsync(string locationName, CancellationToken ct = default)
    {
        if (_driver == null || string.IsNullOrWhiteSpace(locationName)) return;
        var query = "MERGE (l:Location {name: $locationName})";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { locationName = locationName.ToLowerInvariant() });
        });
    }

    public async Task UpsertContentItemAsync(string contentId, string title, string userId, CancellationToken ct = default)
    {
        if (_driver == null) return;
        var query = "MERGE (c:Content {id: $contentId}) SET c.title = $title";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { contentId, title });
            
            // Auto link owner
            var linkQuery = @"
                MATCH (u:User {id: $userId}), (c:Content {id: $contentId})
                MERGE (u)-[:SAVED]->(c)";
            await tx.RunAsync(linkQuery, new { userId, contentId });
        });
    }

    public async Task LinkUserToTopicAsync(string userId, string topicName, string relation = "INTERESTED_IN", int weight = 1, CancellationToken ct = default)
    {
        if (_driver == null || string.IsNullOrWhiteSpace(topicName)) return;
        var query = $@"
            MATCH (u:User {{id: $userId}}), (t:Topic {{name: $topicName}})
            MERGE (u)-[r:{relation}]->(t)
            SET r.weight = $weight";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { userId, topicName = topicName.ToLowerInvariant(), weight });
        });
    }

    public async Task LinkUserToContentAsync(string userId, string contentId, CancellationToken ct = default)
    {
        if (_driver == null) return;
        var query = @"
            MATCH (u:User {id: $userId}), (c:Content {id: $contentId})
            MERGE (u)-[:INTERACTED_WITH]->(c)";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { userId, contentId });
        });
    }

    public async Task LinkContentToLocationAsync(string contentId, string locationName, CancellationToken ct = default)
    {
        if (_driver == null || string.IsNullOrWhiteSpace(locationName)) return;
        var query = @"
            MATCH (c:Content {id: $contentId}), (l:Location {name: $locationName})
            MERGE (c)-[:CONTAINS_LOCATION]->(l)";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { contentId, locationName = locationName.ToLowerInvariant() });
        });
    }

    public async Task LinkContentToTopicAsync(string contentId, string topicName, CancellationToken ct = default)
    {
        if (_driver == null || string.IsNullOrWhiteSpace(topicName)) return;
        var query = @"
            MATCH (c:Content {id: $contentId}), (t:Topic {name: $topicName})
            MERGE (c)-[:MENTIONS_TOPIC]->(t)";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx => {
            await tx.RunAsync(query, new { contentId, topicName = topicName.ToLowerInvariant() });
        });
    }

    public async Task<List<string>> GetRecommendationsForUserAsync(string userId, CancellationToken ct = default)
    {
        if (_driver == null) return new List<string>();
        
        // Multi-hop Graph Traversal:
        // Find content that mentions topics/locations the user is interested in, 
        // OR find content saved by users with similar interests, that this user hasn't saved yet.
        var query = @"
            MATCH (u:User {id: $userId})-[:INTERESTED_IN]->(t:Topic)<-[:MENTIONS_TOPIC]-(c:Content)
            WHERE NOT (u)-[:SAVED]->(c)
            RETURN DISTINCT c.title AS title LIMIT 10
            UNION
            MATCH (u:User {id: $userId})-[:SAVED]->(:Content)-[:CONTAINS_LOCATION]->(l:Location)<-[:CONTAINS_LOCATION]-(c:Content)
            WHERE NOT (u)-[:SAVED]->(c)
            RETURN DISTINCT c.title AS title LIMIT 10
        ";

        var recommendations = new List<string>();
        await using var session = _driver.AsyncSession();
        await session.ExecuteReadAsync(async tx => {
            var cursor = await tx.RunAsync(query, new { userId });
            while (await cursor.FetchAsync())
            {
                recommendations.Add(cursor.Current["title"].As<string>());
            }
        });

        return recommendations;
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver != null)
        {
            await _driver.DisposeAsync();
        }
    }
}
