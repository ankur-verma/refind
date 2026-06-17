using System.Text.Json;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Auth.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Search.Services;

public interface IUserMindsetService
{
    Task<UserMindset> CompileMindsetAsync(Guid userId, CancellationToken ct = default);
}

public class UserMindsetService : IUserMindsetService
{
    private readonly CortexDbContext _context;
    private readonly IAIExtractionService _aiService;
    private readonly ILogger<UserMindsetService> _logger;

    public UserMindsetService(
        CortexDbContext context,
        IAIExtractionService aiService,
        ILogger<UserMindsetService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<UserMindset> CompileMindsetAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Compiling cognitive mindset profile for user: {UserId}", userId);

        // Fetch recent items
        var recentItems = await _context.ContentItems
            .Include(c => c.Payload)
            .Where(c => c.UserId == userId && c.Status == Shared.Enums.ContentStatus.Ready && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(15)
            .ToListAsync(ct);

        // Fetch recent logs
        var recentLogs = await _context.UserActivityLogs
            .Where(l => l.UserId == userId && !l.IsDeleted)
            .OrderByDescending(l => l.CreatedAt)
            .Take(15)
            .ToListAsync(ct);

        var mindset = await _context.UserMindsets.FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted, ct);
        if (mindset is null)
        {
            mindset = new UserMindset { UserId = userId };
            await _context.UserMindsets.AddAsync(mindset, ct);
        }

        if (recentItems.Count == 0 && recentLogs.Count == 0)
        {
            mindset.FocusAreasJson = JsonSerializer.Serialize(new[] { "General learning" });
            mindset.ConsumptionPreference = "Concise";
            mindset.NarrativeSummary = "You are in the initial phase of saving and learning items. Ingest bookmarks or video links to let the AI analyze your focus areas.";
            mindset.LastUpdated = DateTime.UtcNow;
            
            await _context.SaveChangesAsync(ct);
            return mindset;
        }

        // Build prompt context
        var itemsContext = string.Join("\n", recentItems.Select((item, index) => 
            $"- [{item.PlatformType}] {item.Title}: {item.Payload?.QuickSparkSummary}"));

        var logsContext = string.Join("\n", recentLogs.Select(log => 
            $"- {log.CreatedAt:yyyy-MM-dd HH:mm}: {log.Action} (Details: {log.Details})"));

        var systemPrompt = @"You are a cognitive psychology and machine learning profiler for Refind.
Analyze the user's saved items and recent activities to synthesize a 'User Mindset Profile'.
You must understand what topics they are trying to master (focus areas), how they prefer to consume information (consumption preference), and write a narrative external brain summary describing their current learning path.

Provide your response in raw JSON format matching this schema exactly. Do not include any markdown wrap or extra details.
{
  ""focusAreas"": [""string"", ""string"", ...],
  ""consumptionPreference"": ""one of: Concise / Detailed Deep Dive / Action Oriented / Code Heavy"",
  ""narrativeSummary"": ""A detailed, personalized paragraph describing their current learning path, cognitive focus, and interests based on their saved records.""
}";

        var userPrompt = $@"Here is the user's saved memory index:
{itemsContext}

Here is the user's recent activity logs:
{logsContext}

Analyze and compile the JSON mindset profile now.";

        try
        {
            var rawResponse = await _aiService.GetChatCompletionAsync(systemPrompt, userPrompt, ct);
            rawResponse = CleanJsonString(rawResponse);

            using var doc = JsonDocument.Parse(rawResponse);
            var root = doc.RootElement;

            var focusAreas = new List<string>();
            if (root.TryGetProperty("focusAreas", out var focusElement) && focusElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var area in focusElement.EnumerateArray())
                {
                    if (area.ValueKind == JsonValueKind.String)
                        focusAreas.Add(area.GetString()!);
                }
            }

            var consumption = root.TryGetProperty("consumptionPreference", out var consumptionElement)
                ? consumptionElement.GetString() ?? "Concise"
                : "Concise";

            var summary = root.TryGetProperty("narrativeSummary", out var summaryElement)
                ? summaryElement.GetString() ?? "No narrative summary generated."
                : "No narrative summary generated.";

            mindset.FocusAreasJson = JsonSerializer.Serialize(focusAreas);
            mindset.ConsumptionPreference = consumption;
            mindset.NarrativeSummary = summary;
            mindset.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully compiled cognitive profile for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compile cognitive profile for user: {UserId}. Using fallback profile.", userId);
            
            mindset.FocusAreasJson = JsonSerializer.Serialize(new[] { "General learning" });
            mindset.ConsumptionPreference = "Concise";
            mindset.NarrativeSummary = "Successfully compiled fallback profile locally. Save more items and click refresh to trigger deep AI analysis.";
            mindset.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
        }

        return mindset;
    }

    private static string CleanJsonString(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text[7..];
        }
        if (text.EndsWith("```"))
        {
            text = text[..^3];
        }
        return text.Trim();
    }
}
