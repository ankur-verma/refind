using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Search.DTOs;
using Cortex.Modules.Search.Services;
using Cortex.Modules.Search.Entities;
using Cortex.Shared;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Cortex.Modules.Search.UseCases;

public class RAGQueryUseCase
{
    private readonly CortexDbContext _context;
    private readonly IAIExtractionService _aiService;
    private readonly IContentPayloadRepository _payloadRepo;
    private readonly IUserMindsetService _mindsetService;
    private readonly Cortex.Infrastructure.Settings.AIServiceSettings _aiSettings;

    public RAGQueryUseCase(
        CortexDbContext context,
        IAIExtractionService aiService,
        IContentPayloadRepository payloadRepo,
        IUserMindsetService mindsetService,
        Microsoft.Extensions.Options.IOptions<Cortex.Infrastructure.Settings.AIServiceSettings> aiOptions)
    {
        _context = context;
        _aiService = aiService;
        _payloadRepo = payloadRepo;
        _mindsetService = mindsetService;
        _aiSettings = aiOptions.Value;
    }

    public async Task<Result<RAGQueryResponse>> ExecuteAsync(Guid userId, RAGQueryRequest request, CancellationToken ct = default)
    {
        Guard.AgainstNullOrEmpty(request.Query, nameof(request.Query));

        // 1. Fetch relevant memory context
        var sources = new List<RAGSourceDto>();
        var contextText = new List<string>();

        if (request.Mode.Equals("SelectedLinks", StringComparison.OrdinalIgnoreCase))
        {
            if (request.SelectedContentItemIds is null || request.SelectedContentItemIds.Count == 0)
            {
                return Result.Failure<RAGQueryResponse>("Please select one or more links to query.");
            }

            var payloads = await _context.ContentPayloads
                .Include(p => p.ContentItem)
                .Where(p => p.ContentItem.UserId == userId && request.SelectedContentItemIds.Contains(p.ContentItemId) && !p.ContentItem.IsDeleted)
                .ToListAsync(ct);

            foreach (var payload in payloads)
            {
                sources.Add(new RAGSourceDto
                {
                    ContentItemId = payload.ContentItemId,
                    Title = payload.ContentItem.Title,
                    OriginalUrl = payload.ContentItem.OriginalUrl,
                    PlatformType = payload.ContentItem.PlatformType.ToString(),
                    SimilarityScore = 1.0 // selected explicitly
                });

                contextText.Add(FormatContextBlock(payload, request.Query, useFullText: true));
            }
        }
        else
        {
            var matchedPayloads = new List<(ContentPayload Payload, double Similarity)>();

            // Vector Search
            if (request.Mode.Equals("Vector", StringComparison.OrdinalIgnoreCase) || request.Mode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                var embedding = await _aiService.GenerateEmbeddingAsync(request.Query, ct);
                var queryVector = new Vector(embedding);
                var matches = await _payloadRepo.SearchByVectorAsync(userId, queryVector, _aiSettings.ActiveProvider, limit: 5, ct);
                
                foreach (var match in matches)
                {
                    var similarity = Math.Max(0, 1.0 - match.Distance);
                    matchedPayloads.Add((match.Payload, similarity));
                }
            }

            // Keyword Search (Vectorless RAG)
            if (request.Mode.Equals("Vectorless", StringComparison.OrdinalIgnoreCase) || request.Mode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                var keywords = request.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(k => k.Length > 2)
                    .Select(k => k.ToLower())
                    .Take(4)
                    .ToList();

                if (keywords.Count > 0)
                {
                    var keywordMatches = new List<ContentItem>();
                    foreach (var kw in keywords)
                    {
                        var items = await _context.ContentItems
                            .Include(c => c.Payload)
                            .Where(c => c.UserId == userId && c.Payload != null && !c.IsDeleted)
                            .Where(c => c.Title.ToLower().Contains(kw) || c.Payload!.QuickSparkSummary!.ToLower().Contains(kw))
                            .Take(3)
                            .ToListAsync(ct);
                        keywordMatches.AddRange(items);
                    }

                    var distinctItems = keywordMatches.DistinctBy(c => c.Id).Take(5).ToList();
                    foreach (var item in distinctItems)
                    {
                        // Deduplicate if already added in Vector search
                        if (matchedPayloads.Any(p => p.Payload.ContentItemId == item.Id)) continue;
                        
                        matchedPayloads.Add((item.Payload!, 0.75)); // Default keyword matching score
                    }
                }
            }

            // Order by relevance and take top 5
            var topMatches = matchedPayloads.OrderByDescending(x => x.Similarity).Take(5).ToList();
            foreach (var match in topMatches)
            {
                sources.Add(new RAGSourceDto
                {
                    ContentItemId = match.Payload.ContentItemId,
                    Title = match.Payload.ContentItem.Title,
                    OriginalUrl = match.Payload.ContentItem.OriginalUrl,
                    PlatformType = match.Payload.ContentItem.PlatformType.ToString(),
                    SimilarityScore = match.Similarity
                });

                contextText.Add(FormatContextBlock(match.Payload, request.Query, useFullText: false));
            }
        }

        if (sources.Count == 0)
        {
            return Result.Success(new RAGQueryResponse
            {
                Answer = "I couldn't find any relevant items in your stored memory. Ingest some links or try adjusting your search terms.",
                Sources = new List<RAGSourceDto>(),
                MindsetUsed = "None"
            });
        }

        // 1.5. Log search query in UserInteractions table
        var searchInteraction = new UserInteraction
        {
            UserId = userId,
            InteractionType = "Search",
            SearchQuery = request.Query,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserInteractions.Add(searchInteraction);
        await _context.SaveChangesAsync(ct);

        // 2. Fetch or Compile User Mindset
        var mindset = await _context.UserMindsets.FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted, ct);
        if (mindset is null)
        {
            mindset = await _mindsetService.CompileMindsetAsync(userId, ct);
        }

        var focusAreas = JsonSerializer.Deserialize<List<string>>(mindset.FocusAreasJson) ?? new List<string>();

        // Fetch top interest categories and active intents
        var topInterests = await _context.UserInterestProfiles
            .Where(x => x.UserId == userId && !x.IsDeleted && x.Score >= 50)
            .OrderByDescending(x => x.Score)
            .Select(x => $"{x.Category} ({x.Score}%)")
            .ToListAsync(ct);

        var activeIntents = await _context.UserIntents
            .Where(x => x.UserId == userId && !x.IsDeleted && !x.IsResolved)
            .OrderByDescending(x => x.Confidence)
            .Select(x => x.GoalDescription)
            .ToListAsync(ct);

        // 3. Prompt Google Gemini using mindset guidelines
        var systemMessage = $@"You are Antigravity, a personalized learning assistant representing the user's external brain and cognitive memory space.
Your job is to answer the user's query utilizing ONLY the provided Memory Context records below. Do not use outside facts.

Crucially, you must tailor your response style, depth, tone, and formatting to align with the user's current Cognitive Mindset Profile and Interests:
- **Primary Interests / Focus Areas**: {string.Join(", ", focusAreas)}
- **General Long-Term Interests**: {string.Join(", ", topInterests)}
- **Active Goals / Intents**: {string.Join(", ", activeIntents)}
- **Information Preference**: {mindset.ConsumptionPreference} (Concise = short and direct, Detailed Deep Dive = extensive details and steps, Action Oriented = instruction focused, Code Heavy = focus on source code examples)
- **Mindset Narrative**: {mindset.NarrativeSummary}

Citations: Cite the source link/URL inline using markdown when referring to facts from specific documents. Keep answers accurate and refuse to answer if the context contains no relevant info.";

        var userMessage = $@"USER QUERY:
{request.Query}

MEMORY CONTEXT:
---
{string.Join("\n\n---\n\n", contextText)}
---

Formulate your personalized cognitive response now:";

        var answer = await _aiService.GetChatCompletionAsync(systemMessage, userMessage, ct);

        // 4. Record user activity log for mindset tracking
        var log = new UserActivityLog
        {
            UserId = userId,
            Action = "RAG Query",
            Details = $"Query: {request.Query[..Math.Min(100, request.Query.Length)]} | Mode: {request.Mode}"
        };
        await _context.UserActivityLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        return Result.Success(new RAGQueryResponse
        {
            Answer = answer,
            Sources = sources,
            MindsetUsed = $"{mindset.ConsumptionPreference} profile focused on {string.Join(", ", focusAreas.Take(3))}"
        });
    }

    private static string FormatContextBlock(ContentPayload payload, string query, bool useFullText)
    {
        var text = useFullText ? payload.RawText : GetRelevantChunks(payload.RawText, query);
        return $"Title: {payload.ContentItem.Title}\nSource: {payload.ContentItem.OriginalUrl}\nPlatform: {payload.ContentItem.PlatformType}\nSummary: {payload.QuickSparkSummary}\nRaw Text:\n{text}";
    }

    private static string GetRelevantChunks(string text, string query, int maxChars = 4000)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (text.Length <= maxChars) return text;

        // 1. Extract keywords from query
        var queryTerms = query.Split(new[] { ' ', ',', '.', '?', '!', '"', '\'', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 2) // avoid short terms
            .ToHashSet();

        if (queryTerms.Count == 0)
        {
            // No keywords, fall back to first maxChars
            return text[..maxChars] + "... [truncated]";
        }

        // 2. Split text into logical sentences
        var sentences = text.Split(new[] { ". ", "? ", "! " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 5)
            .ToList();

        if (sentences.Count == 0)
        {
            return text[..maxChars] + "... [truncated]";
        }

        // Group sentences into sliding windows of 3 sentences (overlapping by 1 sentence)
        var chunks = new List<(string Content, int Score)>();
        for (int i = 0; i < sentences.Count; i += 2)
        {
            var group = sentences.Skip(i).Take(3).ToList();
            if (group.Count == 0) break;

            var content = string.Join(". ", group) + ".";
            
            // Score based on term matches
            int score = 0;
            foreach (var term in queryTerms)
            {
                if (content.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    score++;
                }
            }

            chunks.Add((content, score));
        }

        // 3. Select top chunks
        var selectedChunks = chunks
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            .Take(5)
            .ToList();

        if (selectedChunks.Count == 0)
        {
            // Fall back to first maxChars if no matching keywords found
            return text[..maxChars] + "... [truncated]";
        }

        // Sort selected chunks by their original order in the video/article to preserve narrative flow
        var orderedSelected = chunks
            .Select((c, idx) => (Chunk: c, Index: idx))
            .Where(x => selectedChunks.Any(sc => sc.Content == x.Chunk.Content))
            .OrderBy(x => x.Index)
            .Select(x => x.Chunk.Content)
            .ToList();

        var result = string.Join("\n... ", orderedSelected);

        // Keep it within maxChars limit
        if (result.Length > maxChars)
        {
            result = result[..maxChars] + "... [truncated]";
        }

        return result;
    }
}
