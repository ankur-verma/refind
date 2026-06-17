using Cortex.Modules.Content.Services;
using Cortex.Modules.Auth.Persistence;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Drip.Persistence;
using Cortex.Database;
using Cortex.Modules.Auth.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Drip.Entities;
using Cortex.Infrastructure.Settings;
using Cortex.Shared.Enums;
using Cortex.Modules.Content.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Cortex.AI.Workers;

/// <summary>
/// Background service that consumes messages from the ai_extraction_queue.
/// Processes content through the AI pipeline: extract → summarize → embed → save.
/// </summary>
public class AIExtractionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly AIServiceSettings _aiSettings;
    private readonly ILogger<AIExtractionWorker> _logger;
    private readonly IHubContext<ContentHub> _hubContext;

    public AIExtractionWorker(
        IServiceProvider serviceProvider,
        IConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> options,
        IOptions<AIServiceSettings> aiOptions,
        ILogger<AIExtractionWorker> logger,
        IHubContext<ContentHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _connectionFactory = connectionFactory;
        _settings = options.Value;
        _aiSettings = aiOptions.Value;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer failed; retrying in {DelaySeconds} seconds", _settings.RetryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _settings.RetryDelaySeconds)), stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken: stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await DeclareTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<AIExtractionMessage>(body);

                if (message is not null)
                    await ProcessContentAsync(message, stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI extraction message; routing to dead-letter queue");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: _settings.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("AI extraction worker is consuming queue {QueueName}", _settings.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _settings.DeadLetterExchange,
            type: "direct",
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: _settings.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: _settings.DeadLetterQueueName,
            exchange: _settings.DeadLetterExchange,
            routingKey: _settings.DeadLetterQueueName,
            cancellationToken: ct);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _settings.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = _settings.DeadLetterQueueName
        };

        await channel.QueueDeclareAsync(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: ct);
    }

    private async Task ProcessContentAsync(AIExtractionMessage message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var contentRepo = scope.ServiceProvider.GetRequiredService<IContentItemRepository>();
        var payloadRepo = scope.ServiceProvider.GetRequiredService<IContentPayloadRepository>();
        var actionRepo = scope.ServiceProvider.GetRequiredService<IActionItemRepository>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<ITagRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var processorFactory = scope.ServiceProvider.GetRequiredService<IContentProcessorFactory>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAIExtractionService>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var contentItem = await contentRepo.GetByIdAsync(message.ContentItemId, ct);
        if (contentItem is null) return;

        try
        {
            // Step 1: Process content using factory-created processor
            ContentProcessingResult result;
            bool isGated = false;
            try
            {
                var platformType = Enum.Parse<PlatformType>(message.PlatformType);
                var processor = processorFactory.CreateProcessor(platformType);
                result = await processor.ProcessAsync(message.Url, ct);

                // If text is extremely short or empty, it could mean paywall or proprietary format
                if (string.IsNullOrWhiteSpace(result.RawText) || result.RawText.Length < 10)
                {
                    isGated = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Content is gated or failed to ingest for URL: {Url}. Flagging for manual review.", message.Url);
                isGated = true;
                result = new ContentProcessingResult
                {
                    Title = ContentProcessorHelpers.CreateTitleFromUrl(message.Url),
                    RawText = string.Empty,
                    SuggestedEnergyLevel = EnergyLevel.BrainDead,
                    EstimatedConsumeTimeMins = 1,
                    SuggestedTags = new List<string> { "gated", "review-required" }
                };
            }

            // Step 2: Update content item metadata
            contentItem.Title = result.Title;
            contentItem.HeroImageUrl = result.HeroImageUrl;
            contentItem.EnergyLevel = result.SuggestedEnergyLevel;
            contentItem.ConsumeTimeMins = result.EstimatedConsumeTimeMins;

            // Step 3: Generate AI summary and embedding
            string summary;
            float[] embedding;

            if (isGated)
            {
                summary = "Requires Manual Review.";
                embedding = await aiService.GenerateEmbeddingAsync("Gated content requires manual review", ct);
            }
            else
            {
                summary = await aiService.GenerateSummaryAsync(result.RawText, ct);
                embedding = await aiService.GenerateEmbeddingAsync(result.RawText, ct);
            }

            // Step 4: Save payload with heavy data
            var payload = await payloadRepo.GetByContentItemIdAsync(contentItem.Id, ct);
            if (payload is null)
            {
                payload = new ContentPayload
                {
                    ContentItemId = contentItem.Id,
                    RawText = result.RawText,
                    QuickSparkSummary = summary
                };
                if (_aiSettings.ActiveProvider == "LocalOllama")
                    payload.OllamaEmbedding = new Vector(embedding);
                else
                    payload.GeminiEmbedding = new Vector(embedding);
                    
                await payloadRepo.AddAsync(payload, ct);
            }
            else
            {
                payload.RawText = result.RawText;
                payload.QuickSparkSummary = summary;
                
                if (_aiSettings.ActiveProvider == "LocalOllama")
                    payload.OllamaEmbedding = new Vector(embedding);
                else
                    payload.GeminiEmbedding = new Vector(embedding);
                    
                await payloadRepo.UpdateAsync(payload, ct);
            }

            // Step 5: Extract action items
            if (!isGated)
            {
                var actions = await aiService.ExtractActionsAsync(result.RawText, ct);
                var existingActions = await actionRepo.GetByContentItemIdAsync(contentItem.Id, ct);
                if (existingActions.Count == 0)
                {
                    var actionItems = actions.Select(a => new ActionItem
                    {
                        ContentItemId = contentItem.Id,
                        Description = a.Description,
                        ItemType = a.Type == "Tool" ? ActionItemType.Tool : ActionItemType.Instruction,
                        SequenceOrder = a.Order
                    }).ToList();
                    await actionRepo.AddRangeAsync(actionItems, ct);
                }
            }

            // Step 6: Create tags
            var existingTags = await tagRepo.GetByContentItemIdAsync(contentItem.Id, ct);
            var existingTagNames = existingTags.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var tagName in result.SuggestedTags)
            {
                if (existingTagNames.Contains(tagName))
                    continue;

                var tag = await tagRepo.GetByNameAsync(tagName, ct);
                if (tag is null)
                {
                    tag = new Tag { Name = tagName.ToLowerInvariant(), IsAIGenerated = true };
                    await tagRepo.AddAsync(tag, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }
                await tagRepo.AddContentItemTagAsync(contentItem.Id, tag.Id, ct);
            }

            contentItem.Status = ContentStatus.Ready;
            await contentRepo.UpdateAsync(contentItem, ct);
            await cache.RemoveByPrefixAsync($"feed:{contentItem.UserId}:", ct);
            await unitOfWork.SaveChangesAsync(ct);
            
            await _hubContext.Clients.User(message.UserId.ToString()).SendAsync("ContentProcessed", new { id = contentItem.Id, status = 1 }, ct);

            _logger.LogInformation("Successfully processed content item {ContentItemId}", contentItem.Id);
        }
        catch (Exception ex)
        {
            contentItem.Status = ContentStatus.Failed;
            await contentRepo.UpdateAsync(contentItem, ct);
            await unitOfWork.SaveChangesAsync(ct);
            
            await _hubContext.Clients.User(message.UserId.ToString()).SendAsync("ContentProcessed", new { id = contentItem.Id, status = 2 }, ct);
            
            _logger.LogError(ex, "Failed to process content item {ContentItemId}", contentItem.Id);
        }
    }
}

public class AIExtractionMessage
{
    public Guid ContentItemId { get; set; }
    public Guid UserId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PlatformType { get; set; } = string.Empty;
}
