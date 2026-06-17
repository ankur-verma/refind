using Cortex.Shared.Enums;

namespace Cortex.Modules.Auth.DTOs;

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public SubscriptionTier Tier { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class UpgradeRequest
{
    public string PaymentToken { get; set; } = string.Empty;
}
