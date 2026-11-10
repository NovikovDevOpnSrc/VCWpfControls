namespace VCWpfControls.Grid.Data;

/// <summary>Filter operator applied to a cell value.</summary>
public enum GridFilterOperator
{
    /// <summary>No filtering.</summary>
    None = 0,

    /// <summary>Cell equals <see cref="GridFilterDescription.Value"/>.</summary>
    Equals = 1,

    /// <summary>Cell does not equal <see cref="GridFilterDescription.Value"/>.</summary>
    NotEquals = 2,

    /// <summary>Cell contains <see cref="GridFilterDescription.Value"/> as text.</summary>
    Contains = 3,

    /// <summary>Cell does not contain <see cref="GridFilterDescription.Value"/> as text.</summary>
    NotContains = 4,

    /// <summary>Cell text starts with <see cref="GridFilterDescription.Value"/>.</summary>
    StartsWith = 5,

    /// <summary>Cell text ends with <see cref="GridFilterDescription.Value"/>.</summary>
    EndsWith = 6,

    /// <summary>Cell &gt; <see cref="GridFilterDescription.Value"/>.</summary>
    GreaterThan = 7,

    /// <summary>Cell &gt;= <see cref="GridFilterDescription.Value"/>.</summary>
    GreaterThanOrEqual = 8,

    /// <summary>Cell &lt; <see cref="GridFilterDescription.Value"/>.</summary>
    LessThan = 9,

    /// <summary>Cell &lt;= <see cref="GridFilterDescription.Value"/>.</summary>
    LessThanOrEqual = 10,

    /// <summary><see cref="GridFilterDescription.Value"/> &lt;= Cell &lt;= <see cref="GridFilterDescription.Value2"/>.</summary>
    Between = 11,

    /// <summary>Cell outside the range [<see cref="GridFilterDescription.Value"/>, <see cref="GridFilterDescription.Value2"/>].</summary>
    NotBetween = 12,

    /// <summary>Cell value is contained in the list <see cref="GridFilterDescription.Value"/> (<see cref="System.Collections.IEnumerable"/>).</summary>
    In = 13,

    /// <summary>Cell value is not contained in the list <see cref="GridFilterDescription.Value"/>.</summary>
    NotIn = 14,

    /// <summary>Cell value is <see langword="null"/> (or <see cref="System.DBNull"/>).</summary>
    IsNull = 15,

    /// <summary>Cell value is not <see langword="null"/>.</summary>
    IsNotNull = 16,

    /// <summary>Custom predicate over the whole data item.</summary>
    Custom = 17,
}

/// <summary>
/// Describes a per-column filter. Filters of a grid are combined with a logical AND.
/// </summary>
public sealed class GridFilterDescription
{
    /// <summary>Creates a filter description.</summary>
    public GridFilterDescription(string fieldName, GridFilterOperator op = GridFilterOperator.None, object? value = null, object? value2 = null)
    {
        FieldName = fieldName;
        Operator = op;
        Value = value;
        Value2 = value2;
    }

    /// <summary>Property path to filter on.</summary>
    public string FieldName { get; set; }

    /// <summary>Filter operator.</summary>
    public GridFilterOperator Operator { get; set; }

    /// <summary>Primary filter operand: value, lower bound (Between) or list (In).</summary>
    public object? Value { get; set; }

    /// <summary>Secondary operand: upper bound for Between/NotBetween.</summary>
    public object? Value2 { get; set; }

    /// <summary>String comparison mode for text operators; defaults to culture-aware, case-insensitive.</summary>
    public StringComparison TextComparison { get; set; } = StringComparison.CurrentCultureIgnoreCase;

    /// <summary>Custom predicate over the whole data item, used by the <see cref="GridFilterOperator.Custom"/> operator.</summary>
    public Predicate<object>? CustomPredicate { get; set; }

    /// <summary>
    /// Evaluates the filter against a data item using the provided accessor
    /// (resolved for the item's type and <see cref="FieldName"/>).
    /// </summary>
    public bool Matches(object item, PropertyAccessor accessor)
        => MatchesCore(item, accessor, Operator, Value, Value2, CustomPredicate, TextComparison);

    /// <summary>
    /// The single evaluation path shared by the grid engine and the condition evaluator
    /// (no description allocation on the hot path).
    /// </summary>
    internal static bool MatchesCore(
        object item,
        PropertyAccessor accessor,
        GridFilterOperator op,
        object? value,
        object? value2,
        Predicate<object>? customPredicate,
        StringComparison textComparison)
    {
        ArgumentNullException.ThrowIfNull(item);

        switch (op)
        {
            case GridFilterOperator.None:
                return true;

            case GridFilterOperator.Custom:
                return customPredicate is null || customPredicate(item);

            case GridFilterOperator.IsNull:
            case GridFilterOperator.IsNotNull:
                var nullCheck = accessor.GetValue(item);
                var isNull = nullCheck is null || nullCheck is DBNull;
                return op == GridFilterOperator.IsNull ? isNull : !isNull;
        }

        var cell = accessor.GetValue(item);
        if (cell is null || cell is DBNull)
        {
            // Null cells only match explicit null operators (handled above) and In with null entry.
            return op is GridFilterOperator.In && ListContains(value, null);
        }

        switch (op)
        {
            case GridFilterOperator.Equals:
                return GridValueCompare.Compare(cell, value, null) == 0;
            case GridFilterOperator.NotEquals:
                return GridValueCompare.Compare(cell, value, null) != 0;
            case GridFilterOperator.Contains:
                return TextOf(cell).Contains(TextOf(value), textComparison);
            case GridFilterOperator.NotContains:
                return !TextOf(cell).Contains(TextOf(value), textComparison);
            case GridFilterOperator.StartsWith:
                return TextOf(cell).StartsWith(TextOf(value), textComparison);
            case GridFilterOperator.EndsWith:
                return TextOf(cell).EndsWith(TextOf(value), textComparison);
            case GridFilterOperator.GreaterThan:
                return GridValueCompare.Compare(cell, value, null) > 0;
            case GridFilterOperator.GreaterThanOrEqual:
                return GridValueCompare.Compare(cell, value, null) >= 0;
            case GridFilterOperator.LessThan:
                return GridValueCompare.Compare(cell, value, null) < 0;
            case GridFilterOperator.LessThanOrEqual:
                return GridValueCompare.Compare(cell, value, null) <= 0;
            case GridFilterOperator.Between:
                return GridValueCompare.Compare(cell, value, null) >= 0
                    && GridValueCompare.Compare(cell, value2, null) <= 0;
            case GridFilterOperator.NotBetween:
                return GridValueCompare.Compare(cell, value, null) < 0
                    || GridValueCompare.Compare(cell, value2, null) > 0;
            case GridFilterOperator.In:
                return ListContains(value, cell);
            case GridFilterOperator.NotIn:
                return !ListContains(value, cell);
            default:
                return true;
        }
    }

    private static string TextOf(object? value) => Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;

    private static bool ListContains(object? list, object? cell)
    {
        if (list is not System.Collections.IEnumerable enumerable || list is string)
        {
            return GridValueCompare.Compare(list, cell, null) == 0;
        }

        foreach (var candidate in enumerable)
        {
            if (GridValueCompare.Compare(candidate, cell, null) == 0)
            {
                return true;
            }
        }

        return false;
    }
}
