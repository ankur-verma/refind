using Cortex.Shared;

namespace Cortex.Modules.Auth.Entities;

/// <summary>
/// Stores the personalized learning/cognitive profile of a user.
/// Analyzed by AI from saved items and search logs, and manually editable.
/// </summary>
public class UserMindset : BaseEntity
{
    public Guid UserId { get; set; }
    
    // Stored as a serialized JSON array of string focus areas
    public string FocusAreasJson { get; set; } = "[]";
    
    public string ConsumptionPreference { get; set; } = "Standard";
    
    public string NarrativeSummary { get; set; } = "No profile compiled yet.";
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}
