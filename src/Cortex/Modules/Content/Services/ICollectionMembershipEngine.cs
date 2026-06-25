using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Modules.Content.Services;

public interface ICollectionMembershipEngine
{
    Task EvaluateItemAsync(Guid contentItemId, CancellationToken ct = default);
    Task EvaluateCollectionAsync(Guid collectionId, CancellationToken ct = default);
}
