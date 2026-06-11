namespace Cortex.SharedKernel;

/// <summary>
/// Extends BaseEntity with audit trail fields.
/// Used for entities that need last-modified tracking.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime? LastModifiedAt { get; set; }
}
