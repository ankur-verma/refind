namespace Cortex.Shared;

/// <summary>
/// Base entity with a UUID primary key and creation timestamp.
/// All domain entities inherit from this.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
