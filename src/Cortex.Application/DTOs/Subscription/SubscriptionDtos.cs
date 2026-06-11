using Cortex.SharedKernel.Enums;

namespace Cortex.Application.DTOs.Subscription;

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
