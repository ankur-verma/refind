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
            var correlationId = Guid.NewGuid().ToString();
            if (ea.BasicProperties.Headers is not null && 
                ea.BasicProperties.Headers.TryGetValue("x-correlation-id", out var traceIdObj) && 
                traceIdObj is byte[] bytes)
            {
                correlationId = Encoding.UTF8.GetString(bytes);
            }

            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
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
            }
        };

        await channel.BasicConsumeAsync(queue: _settings.ExtractionQueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("AI extraction worker is consuming queue {QueueName}", _settings.ExtractionQueueName);

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
            queue: _settings.ExtractionQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: ct);
    }

    private async Task ProcessContentAsync(AIExtractionMessage message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<Cortex.Modules.Content.Services.ContentIngestionPipeline>();
        
        var platformType = Enum.Parse<Cortex.Shared.Enums.PlatformType>(message.PlatformType);
        await pipeline.ProcessAsync(message.ContentItemId, message.UserId, message.Url, platformType, ct);
    }
}

public class AIExtractionMessage
{
    public Guid ContentItemId { get; set; }
    public Guid UserId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PlatformType { get; set; } = string.Empty;
}
