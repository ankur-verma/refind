using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Content.Services;

public class CollectionGenerator : ICollectionGenerator
{
    private readonly CortexDbContext _dbContext;
    private readonly IAIExtractionService _aiService;
    private readonly ICollectionMembershipEngine _membershipEngine;
    private readonly ILogger<CollectionGenerator> _logger;

    public CollectionGenerator(
        CortexDbContext dbContext,
        IAIExtractionService aiService,
        ICollectionMembershipEngine membershipEngine,
        ILogger<CollectionGenerator> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _membershipEngine = membershipEngine;
        _logger = logger;
    }

    public async Task GenerateForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // 1. Gather recent insights for the user to understand their content graph
        var recentInsights = await _dbContext.ContentItems
            .Include(c => c.Insight)
            .Where(c => c.UserId == userId && c.Insight != null)
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .Select(c => new
            {
                c.Insight!.Category,
                c.Insight.SubCategory,
                c.Insight.Intent,
                Topics = c.Insight.Topics
            })
            .ToListAsync(ct);

        if (!recentInsights.Any()) return;

        // Flatten to feed to AI
        var summaryText = JsonSerializer.Serialize(recentInsights);

        // Define a strict prompt to ask for JSON Rules based on this user's data
        var prompt = $@"
Analyze the following content metadata for a user:
{summaryText}

Based on this, propose 2-3 new 'Smart Collections' that would be highly valuable for them. 
Return the output as a strict JSON array of objects matching this C# schema exactly (NO markdown formatting, just raw JSON array):
[
  {{
    ""Name"": ""String (e.g. Places To Visit)"",
    ""Description"": ""String"",
    ""RuleDefinition"": {{
        ""Operator"": ""OR"",
        ""Conditions"": [
            {{ ""Field"": ""Intent"", ""Operator"": ""Contains"", ""Value"": ""Place To Visit"" }}
        ]
    }}
  }}
]
";
        
        // 2. Call AI to generate rules
        var aiResponse = await _aiService.GenerateSummaryAsync(prompt, ct);
        
        // Try to parse the JSON array
        try
        {
            var cleanJson = aiResponse.Replace("```json", "").Replace("```", "").Trim();
            var proposedCollections = JsonSerializer.Deserialize<List<ProposedCollection>>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (proposedCollections == null || !proposedCollections.Any()) return;

            // 3. Save Collections
            var existingNames = await _dbContext.AutoCollections
                .Where(c => c.UserId == userId)
                .Select(c => c.Name.ToLower())
                .ToListAsync(ct);

            foreach (var prop in proposedCollections)
            {
                if (string.IsNullOrWhiteSpace(prop.Name) || existingNames.Contains(prop.Name.ToLower())) continue;

                var ruleJson = JsonSerializer.Serialize(prop.RuleDefinition);
                var collection = new AutoCollection
                {
                    UserId = userId,
                    Name = prop.Name,
                    Description = prop.Description ?? "",
                    RuleDefinition = ruleJson
                };
                
                _dbContext.AutoCollections.Add(collection);
                await _dbContext.SaveChangesAsync(ct);
                
                // Evaluate rules immediately
                await _membershipEngine.EvaluateCollectionAsync(collection.Id, ct);
                existingNames.Add(collection.Name.ToLower());
                
                _logger.LogInformation("Generated auto-collection {Name} for user {UserId}", collection.Name, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI generated collections. AI Response: {Response}", aiResponse);
        }
    }

    private class ProposedCollection
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Domain.Rules.CollectionRuleDefinition? RuleDefinition { get; set; }
    }
}
