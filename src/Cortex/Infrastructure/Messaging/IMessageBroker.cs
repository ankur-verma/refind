

namespace Cortex.Infrastructure.Messaging;

public interface IMessageBroker
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default);
}
