using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Infrastructure.Settings;
using Cortex.Modules.Search.Entities;
using Cortex.Modules.Search.Messaging;
using Cortex.Modules.Search.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cortex.Modules.Search.Workers;

public class BehaviorAnalyticsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<BehaviorAnalyticsWorker> _logger;

    public BehaviorAnalyticsWorker(
        IServiceProvider serviceProvider,
        IConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> options,
        ILogger<BehaviorAnalyticsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _connectionFactory = connectionFactory;
        _settings = options.Value;
        _logger = logger;
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
                _logger.LogError(ex, "RabbitMQ consumer failed in BehaviorAnalyticsWorker; retrying in {DelaySeconds} seconds", _settings.RetryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _settings.RetryDelaySeconds)), stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken: stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await DeclareTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 5, global: false, cancellationToken: stoppingToken);

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
                    var message = JsonSerializer.Deserialize<BehaviorEventMessage>(body);

                    if (message is not null)
                    {
                        await ProcessBehaviorMessageAsync(message, stoppingToken);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing behavior event message; routing to dead-letter queue");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            }
        };

        await channel.BasicConsumeAsync(queue: _settings.BehaviorQueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Behavior Analytics worker is consuming queue {QueueName}", _settings.BehaviorQueueName);

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

        var arguments = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _settings.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = _settings.DeadLetterQueueName
        };

        await channel.QueueDeclareAsync(
            queue: _settings.BehaviorQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: ct);
    }

    private async Task ProcessBehaviorMessageAsync(BehaviorEventMessage message, CancellationToken ct)
    {
        if (!Enum.TryParse<BehaviorEventType>(message.EventType, true, out var eventType))
        {
            _logger.LogWarning("Invalid BehaviorEventType '{EventType}' in message for UserId {UserId}", message.EventType, message.UserId);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var interestEngine = scope.ServiceProvider.GetRequiredService<IInterestEngine>();
        var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();
        
        // Update user's interest profile based on behavior
        await interestEngine.ProcessBehaviorEventAsync(message.UserId, message.ContentItemId, eventType, message.MetadataJson, ct);

        // Dynamically trigger recommendation generation
        try
        {
            await recommendationService.GenerateAndStoreRecommendationsAsync(message.UserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dynamically generate recommendations for user {UserId}", message.UserId);
        }
    }
}
