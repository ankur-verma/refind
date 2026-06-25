using System;
using System.Linq;
using System.Text.Json;
using Cortex.Modules.Content.Entities;

namespace Cortex.Modules.Content.Domain.Rules;

public class RulesEngine : IRulesEngine
{
    public bool Evaluate(ContentItem item, string ruleDefinitionJson)
    {
        if (string.IsNullOrWhiteSpace(ruleDefinitionJson)) return false;
        
        CollectionRuleDefinition? ruleDef;
        try
        {
            ruleDef = JsonSerializer.Deserialize<CollectionRuleDefinition>(ruleDefinitionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return false;
        }

        if (ruleDef == null || ruleDef.Conditions == null || !ruleDef.Conditions.Any()) return false;

        var insight = item.Insight;
        if (insight == null) return false;

        bool evaluateCondition(RuleCondition condition)
        {
            var value = condition.Value?.ToLowerInvariant() ?? "";
            
            switch (condition.Field.ToLowerInvariant())
            {
                case "category":
                    return EvaluateString(insight.Category, condition.Operator, value);
                case "subcategory":
                    return EvaluateString(insight.SubCategory, condition.Operator, value);
                case "intent":
                    return EvaluateString(insight.Intent, condition.Operator, value);
                case "sentiment":
                    return EvaluateString(insight.Sentiment, condition.Operator, value);
                case "topic":
                    return insight.Topics != null && insight.Topics.Any(t => EvaluateString(t, condition.Operator, value));
                case "entity":
                    return insight.Entities != null && insight.Entities.Any(e => e.SemanticEntity != null && EvaluateString(e.SemanticEntity.Name, condition.Operator, value));
                case "location":
                    return insight.Locations != null && insight.Locations.Any(l => l.Location != null && EvaluateString(l.Location.Name, condition.Operator, value));
                case "product":
                    return insight.Products != null && insight.Products.Any(p => p.Product != null && EvaluateString(p.Product.Name, condition.Operator, value));
                case "brand":
                    return insight.Brands != null && insight.Brands.Any(b => b.Brand != null && EvaluateString(b.Brand.Name, condition.Operator, value));
                default:
                    return false;
            }
        }

        if (ruleDef.Operator.Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            return ruleDef.Conditions.Any(evaluateCondition);
        }
        else // Default to AND
        {
            return ruleDef.Conditions.All(evaluateCondition);
        }
    }

    private bool EvaluateString(string? actualValue, string op, string expectedValueLower)
    {
        if (actualValue == null) return false;
        var actualLower = actualValue.ToLowerInvariant();

        return op.ToLowerInvariant() switch
        {
            "equals" => actualLower == expectedValueLower,
            "contains" => actualLower.Contains(expectedValueLower),
            "in" => expectedValueLower.Split(',').Select(x => x.Trim()).Contains(actualLower),
            _ => false
        };
    }
}
