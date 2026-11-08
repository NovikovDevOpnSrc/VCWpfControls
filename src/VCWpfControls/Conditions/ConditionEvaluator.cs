using VCWpfControls.Grid.Data;

namespace VCWpfControls.Conditions;

/// <summary>
/// Evaluates a condition tree against an item (UI-free). Expressions delegate to
/// <see cref="GridFilterDescription.Matches(object, PropertyAccessor)"/> — the exact code
/// the grid engine uses — so a condition tree and the grid view never disagree.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>Whether the item satisfies the tree. Empty groups are And=true / Or=false.</summary>
    public static bool Matches(object item, ConditionNode? node)
    {
        ArgumentNullException.ThrowIfNull(item);
        return node switch
        {
            null => true,
            ConditionExpression expression => MatchesExpression(item, expression),
            ConditionGroup group => MatchesGroup(item, group),
            _ => throw new ArgumentException($"Unknown condition node: {node.GetType().Name}."),
        };
    }

    /// <summary>Whether every item satisfies the tree.</summary>
    public static bool MatchesAll(System.Collections.IEnumerable items, ConditionNode? node)
    {
        foreach (var item in items)
        {
            if (item is not null && !Matches(item, node))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Filters the items down to those satisfying the tree.</summary>
    public static List<object> Where(System.Collections.IEnumerable items, ConditionNode? node)
    {
        var result = new List<object>();
        foreach (var item in items)
        {
            if (item is not null && Matches(item, node))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static bool MatchesGroup(object item, ConditionGroup group)
    {
        if (group.Children.Count == 0)
        {
            // Vacuous truth: an empty And is true, an empty Or is false.
            return group.OperatorKind == ConditionGroupOperator.And;
        }

        if (group.OperatorKind == ConditionGroupOperator.And)
        {
            foreach (var child in group.Children)
            {
                if (!Matches(item, child))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var child in group.Children)
        {
            if (Matches(item, child))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesExpression(object item, ConditionExpression expression)
    {
        var accessor = PropertyAccessor.GetOrCreate(item.GetType(), expression.FieldName);
        var description = new GridFilterDescription(
            expression.FieldName,
            expression.OperatorKind,
            expression.Value,
            expression.Value2);
        return description.Matches(item, accessor);
    }
}
