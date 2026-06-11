namespace Cortex.Domain.Interfaces;

/// <summary>
/// Marker interface for domain events (DDD pattern).
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
