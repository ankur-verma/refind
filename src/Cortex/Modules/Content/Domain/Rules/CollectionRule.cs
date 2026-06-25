using System.Collections.Generic;

namespace Cortex.Modules.Content.Domain.Rules;

public class CollectionRuleDefinition
{
    public string Operator { get; set; } = "AND"; // AND, OR
    public List<RuleCondition> Conditions { get; set; } = new();
}

public class RuleCondition
{
    public string Field { get; set; } = string.Empty; // Category, SubCategory, Intent, Sentiment, Topic, Entity, Location, Product, Brand
    public string Operator { get; set; } = "Equals"; // Equals, Contains, In
    public string Value { get; set; } = string.Empty;
}
