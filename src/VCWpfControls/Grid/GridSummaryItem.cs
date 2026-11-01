using System.ComponentModel.DataAnnotations;
using VCWpfControls.Localization;

namespace VCWpfControls.Grid;

/// <summary>
/// Declares one aggregate shown in the grid footer (and, when grouping is active,
/// next to each group). Add items to <c>GridControl.SummaryItems</c>.
/// </summary>
public sealed class GridSummaryItem
{
    /// <summary>Creates a summary item.</summary>
    public GridSummaryItem(string fieldName, GridSummaryFunction function, string? stringFormat = null)
    {
        FieldName = fieldName;
        Function = function;
        StringFormat = stringFormat;
    }

    /// <summary>Field the aggregate is computed over (ignored for <see cref="GridSummaryFunction.Count"/>).</summary>
    [Display(Description = "Field name to aggregate")]
    public string FieldName { get; set; }

    /// <summary>Aggregate function.</summary>
    public GridSummaryFunction Function { get; set; }

    /// <summary>Optional format applied to the result, e.g. <c>N2</c> or <c>C</c>.</summary>
    public string? StringFormat { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Function}({FieldName})";
}
