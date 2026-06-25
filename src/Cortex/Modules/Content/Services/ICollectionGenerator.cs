using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Modules.Content.Services;

public interface ICollectionGenerator
{
    Task GenerateForUserAsync(Guid userId, CancellationToken ct = default);
}
