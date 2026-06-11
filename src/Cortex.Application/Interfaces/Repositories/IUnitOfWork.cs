namespace Cortex.Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern for transactional consistency.
/// Wraps SaveChanges across multiple repository operations.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
