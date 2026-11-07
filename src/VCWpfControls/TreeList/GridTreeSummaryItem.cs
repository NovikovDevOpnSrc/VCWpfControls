using VCWpfControls.Localization;

namespace VCWpfControls.TreeList;

/// <summary>Which nodes a tree summary aggregates over.</summary>
public enum GridTreeSummaryMode
{
    /// <summary>The whole subtree of the node (all descendants, expanded or not).</summary>
    Subtree = 0,

    /// <summary>Direct children of the node only.</summary>
    Children = 1,
}

/// <summary>
/// One aggregate shown next to a tree node (Sum/Min/Max/Avg/Count over its children).
/// Add items to <see cref="TreeListView.TreeSummaryItems"/>. The computation runs on the
/// FULL tree structure (independent of expansion); <see cref="Level"/> restricts it to one
/// tree level (null = every level).
/// </summary>
public sealed class GridTreeSummaryItem
{
    /// <summary>Creates a tree summary item.</summary>
    public GridTreeSummaryItem(string fieldName, GridSummaryFunction function, string? stringFormat = null)
    {
        FieldName = fieldName;
        Function = function;
        StringFormat = stringFormat;
    }

    /// <summary>Field the aggregate is computed over (ignored for Count).</summary>
    public string FieldName { get; set; }

    /// <summary>Aggregate function.</summary>
    public GridSummaryFunction Function { get; set; }

    /// <summary>Optional format of the result, e.g. <c>N2</c>.</summary>
    public string? StringFormat { get; set; }

    /// <summary>Subtree (default) or direct children only.</summary>
    public GridTreeSummaryMode Mode { get; set; }

    /// <summary>Tree level the summary appears on (null = every level).</summary>
    public int? Level { get; set; }
}
