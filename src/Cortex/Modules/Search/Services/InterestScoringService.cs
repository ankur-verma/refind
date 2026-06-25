using System;
using Cortex.Modules.Search.Entities;

namespace Cortex.Modules.Search.Services;

public interface IInterestScoringService
{
    int GetScoreDelta(BehaviorEventType eventType);
}

public class InterestScoringService : IInterestScoringService
{
    public int GetScoreDelta(BehaviorEventType eventType)
    {
        return eventType switch
        {
            BehaviorEventType.Shared => 10,
            BehaviorEventType.Saved => 5,
            BehaviorEventType.Searched => 4,
            BehaviorEventType.Opened => 2,
            BehaviorEventType.Viewed => 1,
            BehaviorEventType.Revisited => 2,
            BehaviorEventType.Ignored => -5,
            _ => 0
        };
    }
}
