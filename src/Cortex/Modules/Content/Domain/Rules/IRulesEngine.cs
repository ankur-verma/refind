using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Content.Entities;

namespace Cortex.Modules.Content.Domain.Rules;

public interface IRulesEngine
{
    bool Evaluate(ContentItem item, string ruleDefinitionJson);
}
