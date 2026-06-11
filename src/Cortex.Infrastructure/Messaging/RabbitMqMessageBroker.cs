using Cortex.Application.Interfaces.Services;
using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Cortex.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ message broker implementation.
/// Publishes messages for async AI processing.
/// </summary>
public class RabbitMqMessageBroker : IMessageBroker
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqMessageBroker> _logger;

    public RabbitMqMessageBroker(
        IConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMqMessageBroker> logger)
    {
        _connectionFactory = connectionFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken: ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await DeclareTopologyAsync(channel, queueName, ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation("Published message to queue: {QueueName}", queueName);
    }

    private async Task DeclareTopologyAsync(IChannel channel, string queueName, CancellationToken ct)
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
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: ct);
    }
}
